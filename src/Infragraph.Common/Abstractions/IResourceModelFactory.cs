namespace Infragraph.Common.Abstractions;

using Models.Domain;
using Models.Former2;

/// <summary>
/// Factory for creating typed AWS resource models from raw Former2 resources.
/// </summary>
public interface IResourceModelFactory
{
    /// <summary>
    /// Creates a typed AWS resource model from a Former2 resource.
    /// </summary>
    /// <param name="former2Resources">The raw Former2 resource list.</param>
    /// <returns>A typed AWS resource model.</returns>
    ResourceSet CreateResourceSet(ICollection<Former2Resource> former2Resources);

    /// <summary>
    /// Determines if this factory can handle the specified resource type.
    /// </summary>
    /// <param name="resourceType">The Former2 resource type (e.g., "ec2.vpc").</param>
    /// <returns>True if this factory can create a model for the resource type.</returns>
    bool CanHandle(string resourceType);
}

public class ResourceSet
{
    public required List<AwsResource> Resources { get; init; }
    public required Dictionary<string, AwsResource> ResourceIndex { get; init; }
    
    public required List<ResourceRelationship> Relationships { get; init; }
}
