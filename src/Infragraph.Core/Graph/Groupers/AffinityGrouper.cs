using Infragraph.Common.Abstractions;
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

    public IEnumerable<NodeGroup> GroupNodes(
        IEnumerable<GraphNode> nodes,
        IEnumerable<GraphEdge> edges)
    {
        var nodeList = nodes.ToList();
        var edgeList = edges.ToList();

        // Build reverse lookup: target -> list of sources that use it
        var usedBy = new Dictionary<string, List<string>>();
        foreach (var edge in edgeList.Where(e => e.RelationshipType == RelationshipType.Uses))
        {
            if (!usedBy.ContainsKey(edge.Target))
                usedBy[edge.Target] = [];
            usedBy[edge.Target].Add(edge.Source);
        }

        // Build forward lookup for AttachedTo: source -> list of targets
        var attachedTo = new Dictionary<string, List<string>>();
        foreach (var edge in edgeList.Where(e => e.RelationshipType == RelationshipType.AttachedTo))
        {
            if (!attachedTo.ContainsKey(edge.Source))
                attachedTo[edge.Source] = [];
            attachedTo[edge.Source].Add(edge.Target);
        }

        // Find security groups with exactly one user
        foreach (var group in FindSingleUserSecurityGroups(nodeList, usedBy))
            yield return group;

        // Find instance profiles with exactly one user
        foreach (var group in FindSingleUserInstanceProfiles(nodeList, usedBy))
            yield return group;

        // Find volumes attached to exactly one instance
        foreach (var group in FindSingleInstanceVolumes(nodeList, attachedTo))
            yield return group;
    }

    private static IEnumerable<NodeGroup> FindSingleUserSecurityGroups(
        List<GraphNode> nodes,
        Dictionary<string, List<string>> usedBy)
    {
        var securityGroups = nodes.Where(n => n.ResourceType == "ec2.securitygroup").ToList();

        foreach (var sg in securityGroups)
        {
            if (!usedBy.TryGetValue(sg.Id, out var users) || users.Count != 1) continue;
            var consumerId = users[0];
            // Verify the consumer exists in our node list
            if (nodes.Any(n => n.Id == consumerId))
            {
                yield return new NodeGroup
                {
                    Id = $"affinity-sg-{sg.Id}",
                    Label = sg.Label,
                    GroupType = "affinity-hint",
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
        List<GraphNode> nodes,
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
                    Id = $"affinity-profile-{profile.Id}",
                    Label = profile.Label,
                    GroupType = "affinity-hint",
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
        List<GraphNode> nodes,
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
                    Id = $"affinity-volume-{volume.Id}",
                    Label = volume.Label,
                    GroupType = "affinity-hint",
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
