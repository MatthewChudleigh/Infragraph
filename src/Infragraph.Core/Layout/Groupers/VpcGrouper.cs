namespace Infragraph.Core.Layout.Groupers;

using Infragraph.Common.Abstractions;
using Infragraph.Common.Models.Domain;
using Infragraph.Common.Models.Graph;

/// <summary>
/// Groups nodes by VPC and Subnet hierarchy.
/// </summary>
public sealed class VpcGrouper : IGroupingStrategy
{
    public string GroupingType => "vpc";
    public int Priority => 1; // Applied first

    public IEnumerable<NodeGroup> GroupNodes(
        IEnumerable<GraphNode> nodes,
        IEnumerable<GraphEdge> edges)
    {
        var nodeList = nodes.ToList();
        var edgeList = edges.ToList();

        // Build relationship maps
        // containedBy: child -> parent (from Contains relationships)
        // belongsTo: child -> parent (from BelongsTo relationships)
        // attachedTo: source -> target (from AttachedTo relationships)
        var containedBy = new Dictionary<string, string>();
        var belongsTo = new Dictionary<string, string>();
        var attachedTo = new Dictionary<string, List<string>>();

        foreach (var edge in edgeList)
        {
            switch (edge.RelationshipType)
            {
                case RelationshipType.Contains:
                    // Parent contains child
                    containedBy[edge.Target] = edge.Source;
                    break;
                case RelationshipType.BelongsTo:
                    // Child belongs to parent
                    belongsTo[edge.Source] = edge.Target;
                    break;
                case RelationshipType.AttachedTo:
                    // Source attached to target
                    if (!attachedTo.ContainsKey(edge.Source))
                        attachedTo[edge.Source] = new List<string>();
                    attachedTo[edge.Source].Add(edge.Target);
                    break;
            }
        }

        // Helper to check if a node belongs to a VPC or subnet
        bool NodeBelongsTo(string nodeId, string parentId)
        {
            return containedBy.GetValueOrDefault(nodeId) == parentId ||
                   belongsTo.GetValueOrDefault(nodeId) == parentId ||
                   (attachedTo.TryGetValue(nodeId, out var attached) && attached.Contains(parentId));
        }

        // Find VPC nodes
        var vpcNodes = nodeList.Where(n => n.ResourceType == "ec2.vpc").ToList();

        foreach (var vpc in vpcNodes)
        {
            // Find subnets in this VPC
            var subnetsInVpc = nodeList
                .Where(n => n.ResourceType == "ec2.subnet" && NodeBelongsTo(n.Id, vpc.Id))
                .ToList();

            // Track which nodes are assigned to subnets
            var nodesInSubnets = new HashSet<string>();

            // Process subnet groups first to collect their IDs
            var subnetGroups = new List<NodeGroup>();
            var subnetGroupIds = new List<string>();

            foreach (var subnet in subnetsInVpc)
            {
                // Find resources that belong to this subnet
                var resourcesInSubnet = nodeList
                    .Where(n => n.ResourceType != "ec2.subnet" &&
                                n.ResourceType != "ec2.vpc" &&
                                NodeBelongsTo(n.Id, subnet.Id))
                    .Select(n => n.Id)
                    .ToList();

                // Find resources that use this subnet
                var resourcesUsingSubnet = edgeList
                    .Where(e => e.Target == subnet.Id &&
                                e.RelationshipType == RelationshipType.Uses &&
                                !resourcesInSubnet.Contains(e.Source))
                    .Select(e => e.Source)
                    .Where(id => nodeList.Any(n => n.Id == id))
                    .ToList();

                // Include resources in subnet
                resourcesInSubnet.AddRange(resourcesUsingSubnet);
                resourcesInSubnet = resourcesInSubnet.Distinct().ToList();

                // Track nodes assigned to subnets
                foreach (var nodeId in resourcesInSubnet)
                {
                    nodesInSubnets.Add(nodeId);
                }

                if (resourcesInSubnet.Count > 0)
                {
                    var subnetGroup = new NodeGroup
                    {
                        Id = $"group-{subnet.Id}",
                        Label = subnet.Label,
                        GroupType = "subnet",
                        ParentId = $"group-{vpc.Id}",
                        NodeIds = resourcesInSubnet,
                        Data = new Dictionary<string, object>
                        {
                            ["resourceId"] = subnet.Id
                        }
                    };

                    subnetGroups.Add(subnetGroup);
                    subnetGroupIds.Add(subnetGroup.Id);
                }
            }

            // Find security groups that belong to this VPC
            var vpcSecurityGroups = nodeList
                .Where(n => n.ResourceType == "ec2.securitygroup" && NodeBelongsTo(n.Id, vpc.Id))
                .Select(n => n.Id)
                .ToHashSet();

            // Find resources that use security groups belonging to this VPC
            // (These resources should be placed in the VPC if not already in a subnet)
            var resourcesUsingVpcSecurityGroups = edgeList
                .Where(e => e.RelationshipType == RelationshipType.Uses &&
                            vpcSecurityGroups.Contains(e.Target) &&
                            !nodesInSubnets.Contains(e.Source))
                .Select(e => e.Source)
                .Where(id => nodeList.Any(n => n.Id == id &&
                                               n.ResourceType != "ec2.subnet" &&
                                               n.ResourceType != "ec2.vpc"))
                .ToHashSet();

            // Find resources directly in VPC (not in any subnet)
            // This includes security groups, route tables, internet gateways, etc.
            var directVpcResources = nodeList
                .Where(n => n.ResourceType != "ec2.subnet" &&
                            n.ResourceType != "ec2.vpc" &&
                            !nodesInSubnets.Contains(n.Id) &&
                            (NodeBelongsTo(n.Id, vpc.Id) || resourcesUsingVpcSecurityGroups.Contains(n.Id)))
                .Select(n => n.Id)
                .ToList();

            // Create VPC group with collected child group IDs
            var vpcGroup = new NodeGroup
            {
                Id = $"group-{vpc.Id}",
                Label = vpc.Label,
                GroupType = "vpc",
                NodeIds = directVpcResources,
                ChildGroupIds = subnetGroupIds,
                Data = new Dictionary<string, object>
                {
                    ["resourceId"] = vpc.Id
                }
            };

            yield return vpcGroup;

            foreach (var subnetGroup in subnetGroups)
            {
                yield return subnetGroup;
            }
        }
    }
}
