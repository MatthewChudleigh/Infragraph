namespace Infragraph.Core.Parsing;

using System.Runtime.CompilerServices;
using System.Text.Json;
using Infragraph.Common.Abstractions;
using Infragraph.Common.Models.Former2;

/// <summary>
/// Parses Former2 JSON export files.
/// </summary>
public sealed class Former2Parser : IResourceParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public async IAsyncEnumerable<Former2Resource> ParseAsync(
        Stream json,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Former2 exports are JSON arrays of resources
        var document = await JsonDocument.ParseAsync(json, cancellationToken: cancellationToken);

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Former2 JSON must be an array of resources");
        }

        foreach (var element in document.RootElement.EnumerateArray())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var resource = ParseResource(element);
            if (resource != null)
            {
                yield return resource;
            }
        }
    }

    private static Former2Resource? ParseResource(JsonElement element)
    {
        if (!element.TryGetProperty("id", out var idElement) ||
            !element.TryGetProperty("type", out var typeElement))
        {
            return null;
        }

        var id = idElement.GetString();
        var type = typeElement.GetString();

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(type))
        {
            return null;
        }

        string? region = null;
        if (element.TryGetProperty("region", out var regionElement))
        {
            region = regionElement.GetString();
        }

        JsonElement data = default;
        if (element.TryGetProperty("data", out var dataElement))
        {
            data = dataElement.Clone();
        }

        return new Former2Resource
        {
            Id = id,
            Type = type,
            Region = region,
            Data = data
        };
    }
}
