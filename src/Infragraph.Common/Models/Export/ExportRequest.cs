namespace Infragraph.Common.Models.Export;

using System.Text.Json.Serialization;
using ReactFlow;

/// <summary>
/// Request payload for exporting a diagram.
/// </summary>
public sealed class ExportRequest
{
    /// <summary>
    /// The diagram to export (with positions already computed).
    /// </summary>
    [JsonPropertyName("diagram")]
    public required ReactFlowDiagram Diagram { get; init; }

    /// <summary>
    /// Export options.
    /// </summary>
    [JsonPropertyName("options")]
    public ExportOptions Options { get; init; } = new();
}

/// <summary>
/// Options for exporting a diagram.
/// </summary>
public sealed class ExportOptions
{
    /// <summary>
    /// Background color (CSS color string). Default is white.
    /// </summary>
    [JsonPropertyName("backgroundColor")]
    public string BackgroundColor { get; init; } = "#ffffff";

    /// <summary>
    /// Padding around the diagram in pixels.
    /// </summary>
    [JsonPropertyName("padding")]
    public double Padding { get; init; } = 40;

    /// <summary>
    /// Scale factor for the export. Default is 1.0.
    /// </summary>
    [JsonPropertyName("scale")]
    public double Scale { get; init; } = 1.0;

    /// <summary>
    /// Whether to include a watermark/title.
    /// </summary>
    [JsonPropertyName("includeTitle")]
    public bool IncludeTitle { get; init; } = true;

    /// <summary>
    /// Custom title for the diagram. If not provided, uses generated timestamp.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>
    /// Whether to include metadata footer (resource counts, etc.).
    /// </summary>
    [JsonPropertyName("includeMetadata")]
    public bool IncludeMetadata { get; init; } = true;

    /// <summary>
    /// Font family for text. Default is system sans-serif.
    /// </summary>
    [JsonPropertyName("fontFamily")]
    public string FontFamily { get; init; } = "system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif";

    /// <summary>
    /// Whether to include edge labels.
    /// </summary>
    [JsonPropertyName("includeEdgeLabels")]
    public bool IncludeEdgeLabels { get; init; } = true;

    /// <summary>
    /// PNG-specific: Image quality (0.0 to 1.0). Only applies to PNG export.
    /// </summary>
    [JsonPropertyName("quality")]
    public double Quality { get; init; } = 1.0;
}
