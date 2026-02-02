using Infragraph.Core.Graph;

namespace Infragraph.Common.Abstractions;

using Models.Graph;

/// <summary>
/// Strategy for grouping graph nodes into logical groups.
/// </summary>
public interface IGroupingStrategy
{
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
