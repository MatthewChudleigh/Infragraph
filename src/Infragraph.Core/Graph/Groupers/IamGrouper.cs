using Infragraph.Common.Abstractions;
using Infragraph.Common.Models.Domain;
using Infragraph.Common.Models.Graph;

namespace Infragraph.Core.Graph.Groupers;

/// <summary>
/// Groups IAM hierarchies: instance profiles with their roles, roles with their policies.
/// </summary>
public sealed class IamGrouper : IGroupingStrategy
{
    public string GroupingType => "iam";
    public int Priority => 4; // Applied after Affinity grouping

    public IEnumerable<NodeGroup> GroupNodes(
        IEnumerable<GraphNode> nodes,
        IEnumerable<GraphEdge> edges)
    {
        var nodeList = nodes.ToList();
        var edgeList = edges.ToList();

        // Build containment map (parent -> children)
        var contains = new Dictionary<string, List<string>>();
        foreach (var edge in edgeList.Where(e => e.RelationshipType == RelationshipType.Contains))
        {
            if (!contains.ContainsKey(edge.Source))
                contains[edge.Source] = [];
            contains[edge.Source].Add(edge.Target);
        }

        // Build uses map (source -> targets)
        var uses = new Dictionary<string, List<string>>();
        foreach (var edge in edgeList.Where(e => e.RelationshipType == RelationshipType.Uses))
        {
            if (!uses.ContainsKey(edge.Source))
                uses[edge.Source] = [];
            uses[edge.Source].Add(edge.Target);
        }

        // Build assumes map to find role consumers (who assumes the role)
        var assumedBy = new Dictionary<string, List<string>>();
        foreach (var edge in edgeList.Where(e => e.RelationshipType == RelationshipType.Assumes))
        {
            if (!assumedBy.ContainsKey(edge.Target))
                assumedBy[edge.Target] = [];
            assumedBy[edge.Target].Add(edge.Source);
        }

        // Group instance profiles with their contained roles
        foreach (var group in GroupInstanceProfiles(nodeList, contains))
            yield return group;

        // Group roles with their policies (only if single consumer)
        foreach (var group in GroupRolesWithPolicies(nodeList, uses, assumedBy))
            yield return group;
    }

    private static IEnumerable<NodeGroup> GroupInstanceProfiles(
        List<GraphNode> nodes,
        Dictionary<string, List<string>> contains)
    {
        var instanceProfiles = nodes.Where(n => n.ResourceType == "iam.instanceprofile").ToList();

        foreach (var profile in instanceProfiles)
        {
            if (!contains.TryGetValue(profile.Id, out var containedIds)) continue;
            // Find roles contained by this instance profile
            var roleIds = containedIds
                .Where(id => nodes.Any(n => n.Id == id && n.ResourceType == "iam.role"))
                .ToList();

            if (roleIds.Count > 0)
            {
                yield return new NodeGroup
                {
                    Id = $"group-profile-{profile.Id}",
                    Label = profile.Label,
                    GroupType = "instance-profile",
                    NodeIds = roleIds,
                    Data = new Dictionary<string, object>
                    {
                        ["resourceId"] = profile.Id
                    }
                };
            }
        }
    }

    private static IEnumerable<NodeGroup> GroupRolesWithPolicies(
        List<GraphNode> nodes,
        Dictionary<string, List<string>> uses,
        Dictionary<string, List<string>> assumedBy)
    {
        var roles = nodes.Where(n => n.ResourceType == "iam.role").ToList();

        foreach (var role in roles)
        {
            // Skip roles with multiple assumers (shared roles)
            if (assumedBy.TryGetValue(role.Id, out var assumers) && assumers.Count > 1)
                continue;

            if (!uses.TryGetValue(role.Id, out var usedIds)) continue;
            // Find policies used by this role (both managed and inline)
            var policyIds = usedIds
                .Where(id => nodes.Any(n => n.Id == id &&
                                            (n.ResourceType == "iam.policy" ||
                                             n.ResourceType == "iam.managedpolicy" ||
                                             n.ResourceType == "iam.rolepolicy")))
                .ToList();

            // Skip AWS managed policies (they're used by many roles)
            policyIds = policyIds
                .Where(id =>
                {
                    var node = nodes.First(n => n.Id == id);
                    // AWS managed policies typically have ARNs starting with arn:aws:iam::aws:
                    var arn = node.Data.TryGetValue("arn", out var arnValue) ? arnValue.ToString() : null;
                    return arn == null || !arn.StartsWith("arn:aws:iam::aws:");
                })
                .ToList();

            if (policyIds.Count > 0)
            {
                yield return new NodeGroup
                {
                    Id = $"group-role-{role.Id}",
                    Label = role.Label,
                    GroupType = "iam-role",
                    NodeIds = policyIds,
                    Data = new Dictionary<string, object>
                    {
                        ["resourceId"] = role.Id
                    }
                };
            }
        }
    }
}
