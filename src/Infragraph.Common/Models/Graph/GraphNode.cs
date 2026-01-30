namespace Infragraph.Common.Models.Graph;

/// <summary>
/// Represents a node in the infrastructure graph.
/// </summary>
public sealed class GraphNode
{
    /// <summary>
    /// Unique node identifier (typically the resource ID).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display label for the node.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// The AWS resource type (e.g., "ec2.vpc").
    /// </summary>
    public required string ResourceType { get; init; }

    /// <summary>
    /// The AWS service (e.g., "ec2", "iam").
    /// </summary>
    public required string Service { get; init; }

    /// <summary>
    /// The parent group ID if this node is part of a group.
    /// </summary>
    public string? ParentId { get; set; }

    /// <summary>
    /// Additional data to pass to the frontend.
    /// </summary>
    public Dictionary<string, object> Data { get; init; } = [];

    /// <summary>
    /// Node width (optional, for layout).
    /// </summary>
    public double? Width { get; set; }

    /// <summary>
    /// Node height (optional, for layout).
    /// </summary>
    public double? Height { get; set; }
}
