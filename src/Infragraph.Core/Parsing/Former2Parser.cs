using System.Diagnostics.CodeAnalysis;

namespace Infragraph.Core.Parsing;

using System.Runtime.CompilerServices;
using System.Text.Json;
using Common.Abstractions;
using Common.Models.Former2;

/// <summary>
/// Parses Former2 JSON export files.
/// </summary>
public sealed class Former2Parser : IResourceParser
{
    /// <inheritdoc />
    public IAsyncEnumerable<IResourceParser.ParseResult> ParseAsync(Stream json, CancellationToken cancellationToken = default)
    {
        return ParseStreamAsync(json, [], [], cancellationToken: cancellationToken);
    }
    
    public static async IAsyncEnumerable<IResourceParser.ParseResult> ParseStreamAsync(Stream json,
        Dictionary<string, string> accounts, ICollection<string> filterTypes,
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

            var resource = ParseResource(accounts, element);
            if (resource.Result(out var r, out _) &&
                filterTypes.Contains(r.Type))
            {
                continue;
            }
            
            yield return resource;
        }
    }

    private static IResourceParser.ParseResult ParseResource(Dictionary<string, string> accounts, JsonElement element)
    {
        if (!element.TryGetProperty("type", out var typeElement))
        {
            return IResourceParser.ParseResult.Fail(element);
        }
        
        JsonElement? data = null;
        if (element.TryGetProperty("data", out var dataElement))
        {
            data = dataElement.Clone();
        }
        
        var type = typeElement.GetString();
        if (string.IsNullOrEmpty(type))
        {
            return IResourceParser.ParseResult.Fail(element);
        }

        var id = "";
        if (type == "iam.virtualmfadevice" && data != null)
        {
            id = data.Value.GetString("SerialNumber");
        }
        else if (element.TryGetProperty("id", out var idElement))
        {
            id = idElement.GetString();
        }
        else
        {
            return IResourceParser.ParseResult.Fail(element);
        }

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(type))
        {
            return IResourceParser.ParseResult.Fail(element);
        }

        string? region = null;
        if (element.TryGetProperty("region", out var regionElement))
        {
            region = regionElement.GetString();
        }

        string? account = null;
        if ((data?.TryGetProperty("OwnerId", out var ownerId) ?? false)
            && accounts.TryGetValue(ownerId.ToString(), out var ownerAccount))
        {
            account = ownerAccount;
        }
        else if (element.TryGetProperty("account", out var accountElement))
        {
            account = accountElement.GetString();
        }
        
        var resource = new Former2Resource
        {
            Id = id,
            Account = account ?? "",
            Type = type,
            Region = region,
            Data = data ?? default(JsonElement)
        };
        
        return IResourceParser.ParseResult.Ok(resource);
    }
}
