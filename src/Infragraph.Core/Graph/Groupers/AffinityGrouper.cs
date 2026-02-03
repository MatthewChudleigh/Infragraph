using Infragraph.Common.Abstractions;
using Infragraph.Common.Configuration;
using Infragraph.Common.Models.Domain;
using Infragraph.Common.Models.Graph;

namespace Infragraph.Core.Graph.Groupers;

/// <summary>
/// Groups single-user resources (security groups, instance profiles, volumes) with their consumer.
/// Creates affinity hint groups that GraphBuilder will process to co-locate resources.
/// </summary>
public sealed class AffinityGrouper : IGroupingStrategy
{
    public string GroupingType => "affinity";
    public int Priority => 3; // Applied after Service grouping

    public IEnumerable<NodeGroup> GroupNodes(RelationMap map)
    {
        // Find security groups with exactly one user
        foreach (var group in FindSingleUserSecurityGroups(map.Nodes, map.UsedBy))
            yield return group;

        // Find instance profiles with exactly one user
        foreach (var group in FindSingleUserInstanceProfiles(map.Nodes, map.UsedBy))
            yield return group;

        // Find volumes attached to exactly one instance
        foreach (var group in FindSingleInstanceVolumes(map.Nodes, map.AttachedTo))
            yield return group;
    }

    private static IEnumerable<NodeGroup> FindSingleUserSecurityGroups(
        ICollection<GraphNode> nodes,
        Dictionary<string, List<string>> usedBy)
    {
        var securityGroups = nodes.Where(n => n.ResourceType == SupportedResourceTypes.SecurityGroup).ToList();

        foreach (var sg in securityGroups)
        {
            if (!usedBy.TryGetValue(sg.Id, out var users) || users.Count != 1) continue;
            var consumerId = users[0];
            // Verify the consumer exists in our node list
            if (nodes.Any(n => n.Id == consumerId))
            {
                yield return new NodeGroup
                {
                    GroupType = "affinity-hint",
                    Id = $"affinity-sg-{sg.Id}",
                    Label = sg.Label,
                    NodeIds = [sg.Id],
                    Data = new Dictionary<string, object>
                    {
                        ["affinityTarget"] = consumerId,
                        ["resourceType"] = "securitygroup"
                    }
                };
            }
        }
    }

    private static IEnumerable<NodeGroup> FindSingleUserInstanceProfiles(
        ICollection<GraphNode> nodes,
        Dictionary<string, List<string>> usedBy)
    {
        var instanceProfiles = nodes.Where(n => n.ResourceType == "iam.instanceprofile").ToList();

        foreach (var profile in instanceProfiles)
        {
            if (!usedBy.TryGetValue(profile.Id, out var users) || users.Count != 1) continue;
            var consumerId = users[0];
            // Verify the consumer exists in our node list
            if (nodes.Any(n => n.Id == consumerId))
            {
                yield return new NodeGroup
                {
                    GroupType = "affinity-hint",
                    Id = $"affinity-profile-{profile.Id}",
                    Label = profile.Label,
                    NodeIds = [profile.Id],
                    Data = new Dictionary<string, object>
                    {
                        ["affinityTarget"] = consumerId,
                        ["resourceType"] = "instanceprofile"
                    }
                };
            }
        }
    }

    private static IEnumerable<NodeGroup> FindSingleInstanceVolumes(
        ICollection<GraphNode> nodes,
        Dictionary<string, List<string>> attachedTo)
    {
        var volumes = nodes.Where(n => n.ResourceType == "ec2.volume").ToList();

        foreach (var volume in volumes)
        {
            if (!attachedTo.TryGetValue(volume.Id, out var instances) || instances.Count != 1) continue;
            var instanceId = instances[0];
            // Verify the instance exists in our node list
            if (nodes.Any(n => n.Id == instanceId))
            {
                yield return new NodeGroup
                {
                    GroupType = "affinity-hint",
                    Id = $"affinity-volume-{volume.Id}",
                    Label = volume.Label,
                    NodeIds = [volume.Id],
                    Data = new Dictionary<string, object>
                    {
                        ["affinityTarget"] = instanceId,
                        ["resourceType"] = "volume"
                    }
                };
            }
        }
    }
}
