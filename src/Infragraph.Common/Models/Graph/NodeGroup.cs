namespace Infragraph.Common.Models.Graph;

/// <summary>
/// Represents a group of nodes (e.g., VPC, subnet, service cluster).
/// </summary>
public sealed class NodeGroup
{
    /// <summary>
    /// Unique group identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display label for the group.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// The type of grouping (e.g., "vpc", "subnet", "service").
    /// </summary>
    public required string GroupType { get; init; }

    /// <summary>
    /// The parent group ID if this is a nested group.
    /// </summary>
    public string? ParentId { get; set; }

    /// <summary>
    /// IDs of nodes directly contained in this group.
    /// </summary>
    public List<string> NodeIds { get; init; } = [];

    /// <summary>
    /// IDs of child groups (for hierarchical grouping).
    /// </summary>
    public List<string> ChildGroupIds { get; init; } = [];

    /// <summary>
    /// Additional data for rendering.
    /// </summary>
    public Dictionary<string, object> Data { get; init; } = [];
}
