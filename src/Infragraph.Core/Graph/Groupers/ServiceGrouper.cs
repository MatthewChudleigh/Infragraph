using Infragraph.Common.Abstractions;
using Infragraph.Common.Configuration;
using Infragraph.Common.Models.Domain;
using Infragraph.Common.Models.Graph;

namespace Infragraph.Core.Graph.Groupers;

/// <summary>
/// Groups nodes by service (e.g., ECS clusters, ELB load balancers).
/// </summary>
public sealed class ServiceGrouper : IGroupingStrategy
{
    public string GroupingType => "service";
    public int Priority => 2; // Applied after VPC grouping

    public IEnumerable<NodeGroup> GroupNodes(RelationMap map)
    {
        // Group ECS clusters
        foreach (var group in GroupEcsClusters(map.Nodes, map.ContainedBy, map.Uses))
            yield return group;

        // Group Load Balancers with their target groups
        foreach (var group in GroupLoadBalancers(map.Nodes, map.Edges))
            yield return group;
    }

    private static IEnumerable<NodeGroup> GroupEcsClusters(
        ICollection<GraphNode> nodes,
        Dictionary<string, string> containedBy,
        Dictionary<string, List<string>> usesRelationships)
    {
        var clusterNodes = nodes.Where(n => n.ResourceType == SupportedResourceTypes.EcsCluster).ToList();

        foreach (var cluster in clusterNodes)
        {
            // Find services in this cluster
            var servicesInCluster = nodes
                .Where(n => n.ResourceType == SupportedResourceTypes.EcsService && containedBy.GetValueOrDefault(n.Id) == cluster.Id)
                .ToList();

            if (servicesInCluster.Count <= 0) continue;

            var taskDefIds = ExtractTaskDefIds(nodes, usesRelationships, servicesInCluster);
            var serviceIds = servicesInCluster.Select(n => n.Id).ToList();
            var groupNodeIds = serviceIds.Concat(taskDefIds).Distinct().ToList();

            yield return new NodeGroup
            {
                Id = $"group-cluster-{cluster.Id}",
                Label = cluster.Label,
                GroupType = "ecs-cluster",
                NodeIds = groupNodeIds,
                Data = new Dictionary<string, object>
                {
                    ["resourceId"] = cluster.Id
                }
            };
        }
    }

    private static List<string> ExtractTaskDefIds(
        ICollection<GraphNode> nodes, 
        Dictionary<string, List<string>> usesRelationships, 
        List<GraphNode> servicesInCluster)
    {
        // Find task definitions used by these services
        var taskDefIds = new List<string>();
        foreach (var service in servicesInCluster)
        {
            if (!usesRelationships.TryGetValue(service.Id, out var usedResources)) continue;
                    
            var taskDefs = usedResources
                .Where(id => nodes.Any(n => n.Id == id && n.ResourceType == SupportedResourceTypes.EcsTaskDefinition))
                .ToList();
            taskDefIds.AddRange(taskDefs);
        }

        // Combine services and their task definitions
        return taskDefIds;
    }

    private static IEnumerable<NodeGroup> GroupLoadBalancers(
        ICollection<GraphNode> nodes,
        ICollection<GraphEdge> edges)
    {
        var lbNodes = nodes.Where(n => n.ResourceType == SupportedResourceTypes.LoadBalancer).ToList();

        foreach (var lb in lbNodes)
        {
            // Find listeners for this LB
            var listeners = edges
                .Where(e => e.Target == lb.Id &&
                           e.RelationshipType is RelationshipType.BelongsTo or RelationshipType.ListensFor)
                .Select(e => e.Source)
                .Where(id => nodes.Any(n => n.Id == id && n.ResourceType.Contains("listener")))
                .ToList();

            // Find target groups attached to this LB
            var targetGroups = edges
                .Where(e => e.Target == lb.Id && e.RelationshipType == RelationshipType.AttachedTo)
                .Select(e => e.Source)
                .Where(id => nodes.Any(n => n.Id == id && n.ResourceType == "elbv2.targetgroup"))
                .ToList();

            var groupMembers = listeners.Concat(targetGroups).Distinct().ToList();

            if (groupMembers.Count > 0)
            {
                yield return new NodeGroup
                {
                    Id = $"group-lb-{lb.Id}",
                    Label = lb.Label,
                    GroupType = "load-balancer",
                    NodeIds = groupMembers,
                    Data = new Dictionary<string, object>
                    {
                        ["resourceId"] = lb.Id
                    }
                };
            }
        }
    }
}
