using Infragraph.Core.Graph.Groupers;

namespace Infragraph.Core.Graph;

using Common.Abstractions;
using Common.Configuration;
using Common.Models.Domain;
using Infragraph.Common.Models.Graph;

/// <summary>
/// Builds the infrastructure graph from resources and relationships.
/// </summary>
public sealed class GraphBuilder(IEnumerable<IGroupingStrategy> groupingStrategies) : IGraphBuilder
{
    public InfraGraph BuildGraph(
        ResourceSet resourceSet,
        DiagramOptions options)
    {
        var groupingUsed = groupingStrategies
            .Where(g => options.GroupingStrategies.Contains(g.GroupingType));

        var resources =
            from resource in resourceSet.Resources
            where options.IncludeTypes.Count <= 0 || options.IncludeTypes.Contains(resource.Type)
            where !options.ExcludeTypes.Contains(resource.Type)
            where options.IncludeRegions.Count <= 0 || string.IsNullOrEmpty(resource.Region) ||
                  options.IncludeRegions.Contains(resource.Region)
            select resource;

        resourceSet = new ResourceSet()
        {
            Resources = resources.ToList(),
            Relationships = resourceSet.Relationships,
            ResourceIndex = resourceSet.ResourceIndex
        };
        
        return BuildGraph(groupingUsed, resourceSet, options.ShowIsolatedNodes);
    }

    public static IEnumerable<IGroupingStrategy> DefaultGroupingStrategies =>
    [
        new AccountGrouper(),
        new VpcGrouper(),
        new ServiceGrouper(),
        new AffinityGrouper(),
        new IamGrouper(),
    ];
    
    public static InfraGraph BuildGraph(
        IEnumerable<IGroupingStrategy> groupingStrategies,
        ResourceSet resourceSet,
        bool showIsolatedNodes)
    {
        // Build nodes (handle potential duplicates)
        var nodes = BuildNodes(resourceSet.Resources);
        var nodeIndex = new Dictionary<string, GraphNode>();
        foreach (var node in nodes)
        {
            nodeIndex.TryAdd(node.Id, node);
        }

        // Build edges (only for nodes that exist in the graph)
        var edges = BuildEdges(resourceSet.Relationships, nodeIndex);

        // Filter isolated nodes if configured
        if (!showIsolatedNodes)
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
        var map = RelationMap.Map(nodes, edges);
        var (finalNodes, finalEdges, groups) = 
            ApplyGrouping(groupingStrategies, map);

        // Build metadata
        var metadata = BuildMetadata(resourceSet, finalNodes);

        return new InfraGraph
        {
            Nodes = finalNodes,
            Edges = finalEdges,
            Groups = groups,
            Metadata = metadata
        };
    }

    private static List<GraphNode> BuildNodes(List<AwsResource> resources)
    {
        return (
            from resource in resources
            select new GraphNode
            {
                Id = resource.Id,
                Label = resource.DisplayName,
                ResourceType = resource.Type,
                Service = resource.ServiceName,
                Data = new Dictionary<string, object>
                {
                    ["arn"] = resource.Arn ?? resource.Id,
                    ["region"] = resource.Region ?? "global",
                    ["account"] = resource.Account ?? "",
                    ["tags"] = resource.Tags
                }
            }).ToList();
    }

    private static List<GraphEdge> BuildEdges(
        List<ResourceRelationship> relationships,
        Dictionary<string, GraphNode> nodeIndex)
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
        IEnumerable<IGroupingStrategy> groupingStrategies, RelationMap map)
    {
        var allGroups = new List<NodeGroup>();

        foreach (var strategy in groupingStrategies)
        {
            var groups = strategy.GroupNodes(map).ToList();
            allGroups.AddRange(groups);

            // Update node parent IDs based on grouping (skip affinity hints for now)
            foreach (var group in groups.Where(g => g.GroupType != "affinity-hint"))
            {
                foreach (var node in group.NodeIds.Select(nodeId => map.Nodes.FirstOrDefault(n => n.Id == nodeId)))
                {   // Set the node parent if it doesn't already have a parent
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
            var targetNode = map.Nodes.FirstOrDefault(n => n.Id == targetId);
            if (targetNode?.ParentId == null)
                continue;

            // Find the parent group of the target
            var parentGroup = allGroups.FirstOrDefault(g => g.Id == targetNode.ParentId);
            if (parentGroup == null)
                continue;

            // Move affinity nodes into the same parent group
            foreach (var nodeId in hint.NodeIds)
            {
                var node = map.Nodes.FirstOrDefault(n => n.Id == nodeId);
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

            // Assign orphan nodes to their account groups
            var accountGroups = groups.Where(g => g.GroupType == "account").ToDictionary(g => g.Id);
            foreach (var node in map.Nodes.Where(n => n.ParentId == null))
            {
                var account = node.Data.TryGetValue("account", out var acc) ? acc?.ToString() : null;
                if (string.IsNullOrWhiteSpace(account)) continue;

                var accountGroupId = $"group-account-{account}";
                if (!accountGroups.TryGetValue(accountGroupId, out var accountGroup)) continue;

                node.ParentId = accountGroupId;
                if (!accountGroup.NodeIds.Contains(node.Id))
                {
                    accountGroup.NodeIds.Add(node.Id);
                }
            }

            // Build ChildGroupIds for account groups based on groups with account as parent
            foreach (var group in groups.Where(g => g.GroupType != "account" && g.ParentId != null))
            {
                if (!accountGroups.TryGetValue(group.ParentId!, out var accountGroup)) continue;

                if (!accountGroup.ChildGroupIds.Contains(group.Id))
                {
                    accountGroup.ChildGroupIds.Add(group.Id);
                }
            }

            // Remove nodes that are now represented as groups (VPCs, Subnets)
            // These resources are containers, not individual nodes
            var groupResourceIds = groups
                .Where(g => g.Data.ContainsKey("resourceId"))
                .Select(g => g.Data["resourceId"].ToString())
                .Where(id => id != null)
                .ToHashSet();

            var nodes = map.Nodes.Where(n => !groupResourceIds.Contains(n.Id)).ToList();

            // Also remove edges that connect to/from these group resources
            var edges = map.Edges.Where(e =>
                !groupResourceIds.Contains(e.Source) &&
                !groupResourceIds.Contains(e.Target)).ToList();

            return (nodes, edges, groups);
        }
    }

    private static GraphMetadata BuildMetadata(
        ResourceSet resourceSet,
        List<GraphNode> includedNodes)
    {
        return new GraphMetadata
        {
            TotalResources = resourceSet.Resources.Count,
            IncludedResources = includedNodes.Count,
            TotalRelationships = resourceSet.Relationships.Count,
            ResourceTypes = resourceSet.Resources.Select(r => r.Type).Distinct().OrderBy(t => t).ToList(),
            Accounts = resourceSet.Resources
                .Where(r => !string.IsNullOrWhiteSpace(r.Account))
                .Select(r => r.Account)
                .Distinct()
                .OrderBy(a => a)
                .ToList(),
            Regions = resourceSet.Resources
                .Where(r => !string.IsNullOrEmpty(r.Region))
                .Select(r => r.Region!)
                .Distinct()
                .OrderBy(r => r)
                .ToList(),
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }
}
