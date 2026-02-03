namespace Infragraph.Common.Models.Graph;

/// <summary>
/// Represents the complete infrastructure graph model.
/// </summary>
public sealed class InfraGraph
{
    /// <summary>
    /// All nodes in the graph.
    /// </summary>
    public List<GraphNode> Nodes { get; init; } = [];

    /// <summary>
    /// All edges in the graph.
    /// </summary>
    public List<GraphEdge> Edges { get; init; } = [];

    /// <summary>
    /// Node groups for hierarchical layout.
    /// </summary>
    public List<NodeGroup> Groups { get; init; } = [];

    /// <summary>
    /// Metadata about the graph.
    /// </summary>
    public GraphMetadata Metadata { get; init; } = new();
}

/// <summary>
/// Metadata about the infrastructure graph.
/// </summary>
public sealed class GraphMetadata
{
    /// <summary>
    /// Total number of resources parsed.
    /// </summary>
    public int TotalResources { get; set; }

    /// <summary>
    /// Number of resources included in the graph.
    /// </summary>
    public int IncludedResources { get; set; }

    /// <summary>
    /// Number of relationships extracted.
    /// </summary>
    public int TotalRelationships { get; set; }

    /// <summary>
    /// Resource types found in the input.
    /// </summary>
    public List<string> ResourceTypes { get; init; } = [];

    /// <summary>
    /// AWS regions represented in the graph.
    /// </summary>
    public List<string> Regions { get; init; } = [];

    /// <summary>
    /// AWS accounts represented in the graph.
    /// </summary>
    public List<string> Accounts { get; init; } = [];

    /// <summary>
    /// Generation timestamp.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
}
