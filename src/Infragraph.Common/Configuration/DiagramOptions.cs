using Infragraph.Common.Abstractions;

namespace Infragraph.Common.Configuration;

/// <summary>
/// Options for diagram generation.
/// </summary>
public sealed record DiagramOptions
{
    /// <summary>
    /// Resource types to include. If empty, all supported types are included.
    /// </summary>
    public HashSet<string> IncludeTypes { get; init; } = [];

    /// <summary>
    /// Resource types to exclude.
    /// </summary>
    public HashSet<string> ExcludeTypes { get; init; } = [];

    /// <summary>
    /// Regions to include. If empty, all regions are included.
    /// </summary>
    public HashSet<string> IncludeRegions { get; init; } = [];

    /// <summary>
    /// Grouping strategies to apply (e.g., "account", "vpc", "service", "affinity", "iam").
    /// </summary>
    public List<string> GroupingStrategies { get; init; } = [
        IGroupingStrategy.GroupType.Account, 
        IGroupingStrategy.GroupType.Vpc, 
        IGroupingStrategy.GroupType.Service, 
        IGroupingStrategy.GroupType.Affinity, 
        IGroupingStrategy.GroupType.Iam,
        IGroupingStrategy.GroupType.Network
    ];

    /// <summary>
    /// Whether to show isolated nodes (nodes with no relationships).
    /// </summary>
    public bool ShowIsolatedNodes { get; init; } = false;

    /// <summary>
    /// Whether to flatten IAM resources into a single group.
    /// </summary>
    public bool FlattenIam { get; init; } = true;

    /// <summary>
    /// Maximum number of nodes before warning.
    /// </summary>
    public int MaxNodes { get; init; } = 500;

    /// <summary>
    /// Layout direction for ELK.
    /// </summary>
    public LayoutDirection LayoutDirection { get; init; } = LayoutDirection.TopToBottom;

    /// <summary>
    /// Default node width.
    /// </summary>
    public double DefaultNodeWidth { get; init; } = 200;

    /// <summary>
    /// Default node height.
    /// </summary>
    public double DefaultNodeHeight { get; init; } = 60;

    /// <summary>
    /// Creates default options.
    /// </summary>
    public static DiagramOptions Default => new();
}

/// <summary>
/// Layout direction.
/// </summary>
public enum LayoutDirection
{
    /// <summary>
    /// Top to bottom (DOWN).
    /// </summary>
    TopToBottom,

    /// <summary>
    /// Left to right (RIGHT).
    /// </summary>
    LeftToRight,

    /// <summary>
    /// Bottom to top (UP).
    /// </summary>
    BottomToTop,

    /// <summary>
    /// Right to left (LEFT).
    /// </summary>
    RightToLeft
}
