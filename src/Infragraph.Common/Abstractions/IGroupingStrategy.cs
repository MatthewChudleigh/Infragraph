using Infragraph.Core.Graph;

namespace Infragraph.Common.Abstractions;

using Models.Graph;

/// <summary>
/// Strategy for grouping graph nodes into logical groups.
/// </summary>
public interface IGroupingStrategy
{
    public static class GroupType
    {
        public const string Account = "account";
        public const string Vpc = "vpc";
        public const string Service = "service";
        public const string Affinity = "affinity";
        public const string Iam = "iam";
        public const string Network = "network";
    }
    
    /// <summary>
    /// Groups nodes based on the strategy's criteria.
    /// </summary>
    /// <param name="map">The graph nodes, edges and relations to group.</param>
    /// <returns>The node groups.</returns>
    IEnumerable<NodeGroup> GroupNodes(RelationMap map);

    /// <summary>
    /// The type of grouping this strategy provides (e.g., "vpc", "service").
    /// </summary>
    string GroupingType { get; }

    /// <summary>
    /// Priority order for applying grouping (lower = applied first).
    /// </summary>
    int Priority { get; }
}
