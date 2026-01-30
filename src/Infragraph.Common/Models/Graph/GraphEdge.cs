namespace Infragraph.Common.Models.Graph;

using Infragraph.Common.Models.Domain;

/// <summary>
/// Represents an edge (connection) in the infrastructure graph.
/// </summary>
public sealed class GraphEdge
{
    /// <summary>
    /// Unique edge identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Source node ID.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Target node ID.
    /// </summary>
    public required string Target { get; init; }

    /// <summary>
    /// Optional edge label.
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// The relationship type.
    /// </summary>
    public required RelationshipType RelationshipType { get; init; }

    /// <summary>
    /// Whether this edge represents a containment relationship (rendered differently).
    /// </summary>
    public bool IsContainment => RelationshipType == RelationshipType.Contains;

    /// <summary>
    /// Additional data to pass to the frontend.
    /// </summary>
    public Dictionary<string, object> Data { get; init; } = [];
}
