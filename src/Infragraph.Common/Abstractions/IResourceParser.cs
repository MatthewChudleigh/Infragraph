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
    IAsyncEnumerable<Former2Resource> ParseAsync(Stream json, CancellationToken cancellationToken = default);
}
