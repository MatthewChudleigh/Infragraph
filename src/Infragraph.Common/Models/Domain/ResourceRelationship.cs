namespace Infragraph.Common.Models.Domain;

/// <summary>
/// Represents a relationship between two AWS resources.
/// </summary>
public sealed class ResourceRelationship
{
    /// <summary>
    /// The source resource ID.
    /// </summary>
    public required string SourceId { get; init; }

    /// <summary>
    /// The target resource ID.
    /// </summary>
    public required string TargetId { get; init; }

    /// <summary>
    /// The type of relationship.
    /// </summary>
    public required RelationshipType RelationshipType { get; init; }

    /// <summary>
    /// Optional label for the relationship edge.
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// Additional metadata about the relationship.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Types of relationships between AWS resources.
/// </summary>
public enum RelationshipType
{
    /// <summary>
    /// Parent contains child (e.g., VPC contains Subnet).
    /// </summary>
    Contains,

    /// <summary>
    /// Resource belongs to another (e.g., Subnet belongs to VPC).
    /// </summary>
    BelongsTo,

    /// <summary>
    /// Resource uses another (e.g., Service uses TaskDef).
    /// </summary>
    Uses,

    /// <summary>
    /// Resource is attached to another (e.g., Service attached to TargetGroup).
    /// </summary>
    AttachedTo,

    /// <summary>
    /// Resource references another (e.g., SecurityGroup references SecurityGroup).
    /// </summary>
    References,

    /// <summary>
    /// Resource assumes a role (e.g., TaskDef assumes IAM Role).
    /// </summary>
    Assumes,

    /// <summary>
    /// Resource routes to another (e.g., RouteTable routes to Gateway).
    /// </summary>
    RoutesTo,

    /// <summary>
    /// Resource listens for another (e.g., Listener listens for LoadBalancer).
    /// </summary>
    ListensFor,

    /// <summary>
    /// Resource targets another (e.g., Listener targets TargetGroup).
    /// </summary>
    Targets
}
