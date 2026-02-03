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

    public IEnumerable<NodeGroup> GroupNodes(RelationMap map)
    {
        // Group instance profiles with their contained roles
        foreach (var group in GroupInstanceProfiles(map.Nodes, map.Contains))
            yield return group;

        // Group roles with their policies (only if single consumer)
        foreach (var group in GroupRolesWithPolicies(map.Nodes, map.Uses, map.AssumedBy))
            yield return group;
    }

    private static IEnumerable<NodeGroup> GroupInstanceProfiles(
        ICollection<GraphNode> nodes,
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
                // Get the account for this instance profile
                var profileAccount = profile.Data.TryGetValue("account", out var acc) ? acc?.ToString() : null;

                yield return new NodeGroup
                {
                    Id = $"group-profile-{profile.Id}",
                    Label = profile.Label,
                    GroupType = "instance-profile",
                    ParentId = !string.IsNullOrWhiteSpace(profileAccount) ? AccountGrouper.GetAccountGroupId(profileAccount) : null,
                    NodeIds = roleIds,
                    Data = new Dictionary<string, object>
                    {
                        ["resourceId"] = profile.Id,
                        ["account"] = profileAccount ?? ""
                    }
                };
            }
        }
    }

    private static IEnumerable<NodeGroup> GroupRolesWithPolicies(
        ICollection<GraphNode> nodes,
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
                                            n.ResourceType is "iam.policy" or "iam.managedpolicy" or "iam.rolepolicy"))
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
                // Get the account for this role
                var roleAccount = role.Data.TryGetValue("account", out var acc) ? acc?.ToString() : null;

                yield return new NodeGroup
                {
                    Id = $"group-role-{role.Id}",
                    Label = role.Label,
                    GroupType = "iam-role",
                    ParentId = !string.IsNullOrWhiteSpace(roleAccount) ? AccountGrouper.GetAccountGroupId(roleAccount) : null,
                    NodeIds = policyIds,
                    Data = new Dictionary<string, object>
                    {
                        ["resourceId"] = role.Id,
                        ["account"] = roleAccount ?? ""
                    }
                };
            }
        }
    }
}
