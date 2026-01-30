namespace Infragraph.Common.Models.Former2;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a raw resource from a Former2 JSON export.
/// </summary>
public sealed class Former2Resource
{
    /// <summary>
    /// The resource identifier (typically the ARN).
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// The resource type (e.g., "ec2.vpc", "iam.role").
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// The AWS region where the resource exists.
    /// </summary>
    [JsonPropertyName("region")]
    public string? Region { get; init; }

    /// <summary>
    /// The raw resource data as a JSON element for flexible parsing.
    /// </summary>
    [JsonPropertyName("data")]
    public JsonElement Data { get; init; }
}
