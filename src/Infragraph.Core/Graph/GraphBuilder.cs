namespace Infragraph.Core.Graph;

using Common.Abstractions;
using Common.Configuration;
using Common.Models.Domain;
using Infragraph.Common.Models.Graph;

/// <summary>
/// Builds the infrastructure graph from resources and relationships.
/// </summary>
public sealed class GraphBuilder : IGraphBuilder
{
    public InfraGraph BuildGraph(
        IEnumerable<AwsResource> resources,
        IEnumerable<ResourceRelationship> relationships,
        IEnumerable<IGroupingStrategy> groupingStrategies,
        DiagramOptions options)
    {
        return BuildGraph(groupingStrategies, resources, relationships, options);
    }
    
    private static InfraGraph BuildGraph(
        IEnumerable<IGroupingStrategy> groupingStrategies,
        IEnumerable<AwsResource> resources,
        IEnumerable<ResourceRelationship> relationships,
        DiagramOptions options)
    {
        var resourceList = resources.ToList();
        var relationshipList = relationships.ToList();

        // Build nodes (handle potential duplicates)
        var nodes = BuildNodes(resourceList, options);
        var nodeIndex = new Dictionary<string, GraphNode>();
        foreach (var node in nodes)
        {
            nodeIndex.TryAdd(node.Id, node);
        }

        // Build edges (only for nodes that exist in the graph)
        var edges = BuildEdges(relationshipList, nodeIndex, options);

        // Filter isolated nodes if configured
        if (!options.ShowIsolatedNodes)
        {
            var connectedNodeIds = new HashSet<string>();
            foreach (var edge in edges)
            {
                connectedNodeIds.Add(edge.Source);
                connectedNodeIds.Add(edge.Target);
            }

            nodes = nodes.Where(n => connectedNodeIds.Contains(n.Id)).ToList();
        }

        // Apply grouping strategies
        var (finalNodes, finalEdges, groups) = 
            ApplyGrouping(groupingStrategies, nodes, edges, options);

        // Build metadata
        var metadata = BuildMetadata(resourceList, relationshipList, finalNodes, options);

        return new InfraGraph
        {
            Nodes = finalNodes,
            Edges = finalEdges,
            Groups = groups,
            Metadata = metadata
        };
    }

    private static List<GraphNode> BuildNodes(List<AwsResource> resources, DiagramOptions options)
    {
        return (from resource in resources
            where options.IncludeTypes.Count <= 0 || options.IncludeTypes.Contains(resource.Type)
            where !options.ExcludeTypes.Contains(resource.Type)
            where options.IncludeRegions.Count <= 0 || string.IsNullOrEmpty(resource.Region) || options.IncludeRegions.Contains(resource.Region)
            select new GraphNode
            {
                Id = resource.Id,
                Label = resource.DisplayName,
                ResourceType = resource.Type,
                Service = resource.ServiceName,
                Width = options.DefaultNodeWidth,
                Height = options.DefaultNodeHeight,
                Data = new Dictionary<string, object>
                {
                    ["arn"] = resource.Arn ?? resource.Id, 
                    ["region"] = resource.Region ?? "global", 
                    ["tags"] = resource.Tags
                }
            }).ToList();
    }

    private static List<GraphEdge> BuildEdges(
        List<ResourceRelationship> relationships,
        Dictionary<string, GraphNode> nodeIndex,
        DiagramOptions options) // TODO: options?
    {
        var edges = new List<GraphEdge>();
        var seenEdges = new HashSet<string>();

        foreach (var edge in from rel in relationships 
                 where nodeIndex.ContainsKey(rel.SourceId) && nodeIndex.ContainsKey(rel.TargetId) 
                 let edgeKey = $"{rel.SourceId}|{rel.TargetId}|{rel.RelationshipType}" 
                 where seenEdges.Add(edgeKey) 
                 select new GraphEdge
                 {
                     Id = $"e-{edges.Count}",
                     Source = rel.SourceId,
                     Target = rel.TargetId,
                     Label = rel.Label,
                     RelationshipType = rel.RelationshipType,
                     Data = new Dictionary<string, object>
                     {
                         ["relationshipType"] = rel.RelationshipType.ToString()
                     }
                 })
        {
            edges.Add(edge);
        }

        return edges;
    }

    private static (List<GraphNode>, List<GraphEdge>, List<NodeGroup>) ApplyGrouping(
        IEnumerable<IGroupingStrategy> groupingStrategies,
        List<GraphNode> nodes,
        List<GraphEdge> edges,
        DiagramOptions options)
    {
        var allGroups = new List<NodeGroup>();

        foreach (var strategy in groupingStrategies)
        {
            if (!options.GroupingStrategies.Contains(strategy.GroupingType))
                continue;

            var groups = strategy.GroupNodes(nodes, edges).ToList();
            allGroups.AddRange(groups);

            // Update node parent IDs based on grouping (skip affinity hints for now)
            foreach (var group in groups.Where(g => g.GroupType != "affinity-hint"))
            {
                foreach (var node in group.NodeIds.Select(nodeId => nodes.FirstOrDefault(n => n.Id == nodeId)))
                {
                    if (node is { ParentId: null })
                    {
                        node.ParentId = group.Id;
                    }
                }
            }
        }

        // Process affinity hints: move nodes into the same group as their target
        var affinityHints = allGroups.Where(g => g.GroupType == "affinity-hint").ToList();
        foreach (var hint in affinityHints)
        {
            if (!hint.Data.TryGetValue("affinityTarget", out var targetObj) || targetObj is not string targetId)
                continue;

            // Find the target node and its parent group
            var targetNode = nodes.FirstOrDefault(n => n.Id == targetId);
            if (targetNode?.ParentId == null)
                continue;

            // Find the parent group of the target
            var parentGroup = allGroups.FirstOrDefault(g => g.Id == targetNode.ParentId);
            if (parentGroup == null)
                continue;

            // Move affinity nodes into the same parent group
            foreach (var nodeId in hint.NodeIds)
            {
                var node = nodes.FirstOrDefault(n => n.Id == nodeId);
                if (node == null) continue;
                node.ParentId = parentGroup.Id;

                // Add to parent group's node list if not already there
                if (!parentGroup.NodeIds.Contains(nodeId))
                {
                    parentGroup.NodeIds.Add(nodeId);
                }
            }
        }

        {
            // Filter out affinity hint groups from final output
            var groups = allGroups.Where(g => g.GroupType != "affinity-hint").ToList();

            // Remove nodes that are now represented as groups (VPCs, Subnets)
            // These resources are containers, not individual nodes
            var groupResourceIds = groups
                .Where(g => g.Data.ContainsKey("resourceId"))
                .Select(g => g.Data["resourceId"].ToString())
                .Where(id => id != null)
                .ToHashSet();

            nodes = nodes.Where(n => !groupResourceIds.Contains(n.Id)).ToList();

            // Also remove edges that connect to/from these group resources
            edges = edges.Where(e =>
                !groupResourceIds.Contains(e.Source) &&
                !groupResourceIds.Contains(e.Target)).ToList();

            return (nodes, edges, groups);
        }
    }

    private static GraphMetadata BuildMetadata(
        List<AwsResource> allResources,
        List<ResourceRelationship> allRelationships,
        List<GraphNode> includedNodes,
        DiagramOptions options) // TODO: options?
    {
        return new GraphMetadata
        {
            TotalResources = allResources.Count,
            IncludedResources = includedNodes.Count,
            TotalRelationships = allRelationships.Count,
            ResourceTypes = allResources.Select(r => r.Type).Distinct().OrderBy(t => t).ToList(),
            Regions = allResources
                .Where(r => !string.IsNullOrEmpty(r.Region))
                .Select(r => r.Region!)
                .Distinct()
                .OrderBy(r => r)
                .ToList(),
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }
}
