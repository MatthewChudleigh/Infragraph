namespace Infragraph.Common.Abstractions;

using Infragraph.Common.Models.Graph;

/// <summary>
/// Strategy for grouping graph nodes into logical groups.
/// </summary>
public interface IGroupingStrategy
{
    /// <summary>
    /// Groups nodes based on the strategy's criteria.
    /// </summary>
    /// <param name="nodes">The graph nodes to group.</param>
    /// <param name="edges">The edges between nodes (may influence grouping).</param>
    /// <returns>The node groups.</returns>
    IEnumerable<NodeGroup> GroupNodes(
        IEnumerable<GraphNode> nodes,
        IEnumerable<GraphEdge> edges);

    /// <summary>
    /// The type of grouping this strategy provides (e.g., "vpc", "service").
    /// </summary>
    string GroupingType { get; }

    /// <summary>
    /// Priority order for applying grouping (lower = applied first).
    /// </summary>
    int Priority { get; }
}
