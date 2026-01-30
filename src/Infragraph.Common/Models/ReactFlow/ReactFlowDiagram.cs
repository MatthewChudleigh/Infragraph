namespace Infragraph.Common.Models.ReactFlow;

using System.Text.Json.Serialization;

/// <summary>
/// React Flow diagram output format.
/// </summary>
public sealed class ReactFlowDiagram
{
    /// <summary>
    /// React Flow nodes.
    /// </summary>
    [JsonPropertyName("nodes")]
    public List<ReactFlowNode> Nodes { get; init; } = [];

    /// <summary>
    /// React Flow edges.
    /// </summary>
    [JsonPropertyName("edges")]
    public List<ReactFlowEdge> Edges { get; init; } = [];

    /// <summary>
    /// Diagram metadata.
    /// </summary>
    [JsonPropertyName("metadata")]
    public ReactFlowMetadata Metadata { get; init; } = new();
}

/// <summary>
/// React Flow node.
/// </summary>
public sealed class ReactFlowNode
{
    /// <summary>
    /// Unique node ID.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Node type for custom rendering (e.g., "awsResource", "awsGroup").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "default";

    /// <summary>
    /// Node position (initially unset, computed by ELK.js on client).
    /// </summary>
    [JsonPropertyName("position")]
    public ReactFlowPosition Position { get; init; } = new();

    /// <summary>
    /// Node data payload.
    /// </summary>
    [JsonPropertyName("data")]
    public required ReactFlowNodeData Data { get; init; }

    /// <summary>
    /// Parent node ID for nested nodes (groups).
    /// </summary>
    [JsonPropertyName("parentId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ParentId { get; init; }

    /// <summary>
    /// Extent for nested nodes.
    /// </summary>
    [JsonPropertyName("extent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Extent { get; init; }

    /// <summary>
    /// Node width.
    /// </summary>
    [JsonPropertyName("width")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Width { get; init; }

    /// <summary>
    /// Node height.
    /// </summary>
    [JsonPropertyName("height")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Height { get; init; }

    /// <summary>
    /// Style overrides.
    /// </summary>
    [JsonPropertyName("style")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Style { get; init; }
}

/// <summary>
/// React Flow node position.
/// </summary>
public sealed class ReactFlowPosition
{
    /// <summary>
    /// X coordinate.
    /// </summary>
    [JsonPropertyName("x")]
    public double X { get; init; }

    /// <summary>
    /// Y coordinate.
    /// </summary>
    [JsonPropertyName("y")]
    public double Y { get; init; }
}

/// <summary>
/// React Flow node data.
/// </summary>
public sealed class ReactFlowNodeData
{
    /// <summary>
    /// Display label.
    /// </summary>
    [JsonPropertyName("label")]
    public required string Label { get; init; }

    /// <summary>
    /// AWS resource type (e.g., "ec2.vpc").
    /// </summary>
    [JsonPropertyName("resourceType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ResourceType { get; init; }

    /// <summary>
    /// AWS service name (e.g., "ec2").
    /// </summary>
    [JsonPropertyName("service")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Service { get; init; }

    /// <summary>
    /// Resource ARN.
    /// </summary>
    [JsonPropertyName("arn")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Arn { get; init; }

    /// <summary>
    /// Group type for group nodes.
    /// </summary>
    [JsonPropertyName("groupType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GroupType { get; init; }

    /// <summary>
    /// Whether this is a group/container node.
    /// </summary>
    [JsonPropertyName("isGroup")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsGroup { get; init; }

    /// <summary>
    /// Additional custom data.
    /// </summary>
    [JsonPropertyName("extra")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Extra { get; init; }
}

/// <summary>
/// React Flow edge.
/// </summary>
public sealed class ReactFlowEdge
{
    /// <summary>
    /// Unique edge ID.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Source node ID.
    /// </summary>
    [JsonPropertyName("source")]
    public required string Source { get; init; }

    /// <summary>
    /// Target node ID.
    /// </summary>
    [JsonPropertyName("target")]
    public required string Target { get; init; }

    /// <summary>
    /// Edge type (e.g., "default", "smoothstep", "bezier").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "smoothstep";

    /// <summary>
    /// Edge label.
    /// </summary>
    [JsonPropertyName("label")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Label { get; init; }

    /// <summary>
    /// Whether the edge is animated.
    /// </summary>
    [JsonPropertyName("animated")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Animated { get; init; }

    /// <summary>
    /// Edge style.
    /// </summary>
    [JsonPropertyName("style")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Style { get; init; }

    /// <summary>
    /// Edge data.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ReactFlowEdgeData? Data { get; init; }

    /// <summary>
    /// Marker at the end of the edge.
    /// </summary>
    [JsonPropertyName("markerEnd")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ReactFlowMarker? MarkerEnd { get; init; }
}

/// <summary>
/// React Flow edge data.
/// </summary>
public sealed class ReactFlowEdgeData
{
    /// <summary>
    /// Relationship type.
    /// </summary>
    [JsonPropertyName("relationshipType")]
    public string? RelationshipType { get; init; }
}

/// <summary>
/// React Flow edge marker.
/// </summary>
public sealed class ReactFlowMarker
{
    /// <summary>
    /// Marker type.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "arrowclosed";

    /// <summary>
    /// Marker width.
    /// </summary>
    [JsonPropertyName("width")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Width { get; init; }

    /// <summary>
    /// Marker height.
    /// </summary>
    [JsonPropertyName("height")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Height { get; init; }

    /// <summary>
    /// Marker color.
    /// </summary>
    [JsonPropertyName("color")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Color { get; init; }
}

/// <summary>
/// Diagram metadata.
/// </summary>
public sealed class ReactFlowMetadata
{
    /// <summary>
    /// Total resources in input.
    /// </summary>
    [JsonPropertyName("totalResources")]
    public int TotalResources { get; init; }

    /// <summary>
    /// Resources included in diagram.
    /// </summary>
    [JsonPropertyName("includedResources")]
    public int IncludedResources { get; init; }

    /// <summary>
    /// Number of relationships.
    /// </summary>
    [JsonPropertyName("totalRelationships")]
    public int TotalRelationships { get; init; }

    /// <summary>
    /// Resource types present.
    /// </summary>
    [JsonPropertyName("resourceTypes")]
    public List<string> ResourceTypes { get; init; } = [];

    /// <summary>
    /// AWS regions present.
    /// </summary>
    [JsonPropertyName("regions")]
    public List<string> Regions { get; init; } = [];

    /// <summary>
    /// Generation timestamp.
    /// </summary>
    [JsonPropertyName("generatedAt")]
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// ELK layout options for the frontend.
    /// </summary>
    [JsonPropertyName("elkOptions")]
    public ElkLayoutOptions ElkOptions { get; init; } = new();
}

/// <summary>
/// ELK.js layout options to be applied on the client.
/// </summary>
public sealed class ElkLayoutOptions
{
    /// <summary>
    /// Layout algorithm.
    /// </summary>
    [JsonPropertyName("algorithm")]
    public string Algorithm { get; init; } = "layered";

    /// <summary>
    /// Direction of the layout.
    /// </summary>
    [JsonPropertyName("direction")]
    public string Direction { get; init; } = "DOWN";

    /// <summary>
    /// Spacing between nodes.
    /// </summary>
    [JsonPropertyName("nodeSpacing")]
    public double NodeSpacing { get; init; } = 50;

    /// <summary>
    /// Spacing between layers.
    /// </summary>
    [JsonPropertyName("layerSpacing")]
    public double LayerSpacing { get; init; } = 100;
}
