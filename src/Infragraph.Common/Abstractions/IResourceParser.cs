using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Infragraph.Common.Abstractions;

using Models.Former2;

/// <summary>
/// Parses Former2 JSON export files into structured resource objects.
/// </summary>
public interface IResourceParser
{
    /// <summary>
    /// Asynchronously parses a Former2 JSON stream, yielding resources as they are parsed.
    /// </summary>
    /// <param name="json">The input stream containing Former2 JSON.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of parsed Former2 resources.</returns>
    IAsyncEnumerable<ParseResult> ParseAsync(Stream json, CancellationToken cancellationToken = default);
    
    public record ParseResult(
        bool Success,
        Former2Resource? Resource,
        JsonElement? Invalid)
    {
        public static ParseResult Ok(Former2Resource resource)
        {
            return new ParseResult(true, resource, null);
        }

        public static ParseResult Fail(JsonElement invalid)
        {
            return new  ParseResult(false, null, invalid);
        }
        
        public bool Result([NotNullWhen(true)] out Former2Resource? resource,
            [NotNullWhen(false)] out JsonElement? invalid)
        {
            if (Success)
            {
                invalid = null;
                resource = Resource!;
                return true;
            }
            else
            {
                resource = null;
                invalid = Invalid!;
                return false;
            }
        }
    }
}
