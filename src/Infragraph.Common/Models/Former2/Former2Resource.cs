using System.Text.Json.Serialization.Metadata;

namespace Infragraph.Common.Models.Former2;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a raw resource from a Former2 JSON export.
/// </summary>
[JsonSerializable(typeof(Former2Resource))]
[JsonSerializable(typeof(List<Former2Resource>))]
public partial class Former2JsonContext : JsonSerializerContext
{
    public static async Task SerializeAsync(Stream stream, List<Former2Resource> resources,
        CancellationToken cancel)
    {
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = JsonTypeInfoResolver.Combine(Default),
            WriteIndented = true,
            NewLine = "\n",
        };
        await JsonSerializer.SerializeAsync(stream, resources, options, cancel);
    }
}

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
    /// The AWS acount where the resource exists.
    /// </summary>
    [JsonPropertyName("account")]
    public required string Account { get; set; }

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

public static class JsonElementExtensions
{
    public static string? GetString(this JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;
    
    public static string? GetNestedString(this JsonElement data, string prop, string nested) =>
        data.TryGetProperty(prop, out var obj) && obj.TryGetProperty(nested, out var val) && val.ValueKind == JsonValueKind.String
            ? val.GetString() : null;

    public static int? GetIntNullable(this JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number ? val.GetInt32() : null;

    public static bool GetBool(this JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.True;

    public static int GetInt(this JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number ? val.GetInt32() : 0;
}