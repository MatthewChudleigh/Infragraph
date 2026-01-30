namespace Infragraph.Common.Abstractions;

using Infragraph.Common.Models.Domain;

/// <summary>
/// Extracts relationships between AWS resources.
/// </summary>
public interface IRelationshipExtractor
{
    /// <summary>
    /// Extracts relationships from a source resource to other resources in the index.
    /// </summary>
    /// <param name="source">The source resource to extract relationships from.</param>
    /// <param name="index">An index of all resources by their ID.</param>
    /// <returns>The extracted relationships.</returns>
    IEnumerable<ResourceRelationship> ExtractRelationships(
        AwsResource source,
        IReadOnlyDictionary<string, AwsResource> index);

    /// <summary>
    /// The resource types this extractor supports.
    /// </summary>
    IEnumerable<string> SupportedResourceTypes { get; }
}
