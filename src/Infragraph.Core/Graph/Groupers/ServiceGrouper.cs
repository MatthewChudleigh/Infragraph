using Infragraph.Common.Abstractions;
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

    public IEnumerable<NodeGroup> GroupNodes(
        IEnumerable<GraphNode> nodes,
        IEnumerable<GraphEdge> edges)
    {
        var nodeList = nodes.ToList();
        var edgeList = edges.ToList();

        // Find containment relationships
        var containedBy = new Dictionary<string, string>();
        foreach (var edge in edgeList.Where(e => e.RelationshipType == RelationshipType.Contains))
        {
            containedBy[edge.Target] = edge.Source;
        }

        // Build uses relationships map (source -> list of targets)
        var usesRelationships = new Dictionary<string, List<string>>();
        foreach (var edge in edgeList.Where(e => e.RelationshipType == RelationshipType.Uses))
        {
            if (!usesRelationships.ContainsKey(edge.Source))
                usesRelationships[edge.Source] = [];
            usesRelationships[edge.Source].Add(edge.Target);
        }

        // Group ECS clusters
        foreach (var group in GroupEcsClusters(nodeList, containedBy, usesRelationships))
            yield return group;

        // Group Load Balancers with their target groups
        foreach (var group in GroupLoadBalancers(nodeList, edgeList))
            yield return group;
    }

    private static IEnumerable<NodeGroup> GroupEcsClusters(
        List<GraphNode> nodes,
        Dictionary<string, string> containedBy,
        Dictionary<string, List<string>> usesRelationships)
    {
        var clusterNodes = nodes.Where(n => n.ResourceType == "ecs.cluster").ToList();

        foreach (var cluster in clusterNodes)
        {
            // Find services in this cluster
            var servicesInCluster = nodes
                .Where(n => n.ResourceType == "ecs.service" && containedBy.GetValueOrDefault(n.Id) == cluster.Id)
                .ToList();

            if (servicesInCluster.Count > 0)
            {
                var serviceIds = servicesInCluster.Select(n => n.Id).ToList();

                // Find task definitions used by these services
                var taskDefIds = new List<string>();
                foreach (var service in servicesInCluster)
                {
                    if (usesRelationships.TryGetValue(service.Id, out var usedResources))
                    {
                        var taskDefs = usedResources
                            .Where(id => nodes.Any(n => n.Id == id && n.ResourceType == "ecs.taskdefinition"))
                            .ToList();
                        taskDefIds.AddRange(taskDefs);
                    }
                }

                // Combine services and their task definitions
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
    }

    private static IEnumerable<NodeGroup> GroupLoadBalancers(
        List<GraphNode> nodes,
        List<GraphEdge> edges)
    {
        var lbNodes = nodes.Where(n => n.ResourceType == "elbv2.loadbalancer").ToList();

        foreach (var lb in lbNodes)
        {
            // Find listeners for this LB
            var listeners = edges
                .Where(e => e.Target == lb.Id &&
                           (e.RelationshipType == RelationshipType.BelongsTo ||
                            e.RelationshipType == RelationshipType.ListensFor))
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
