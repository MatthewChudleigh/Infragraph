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
    /// <param name="resource">The raw Former2 resource.</param>
    /// <returns>A typed AWS resource model.</returns>
    AwsResource CreateModel(Former2Resource resource);

    /// <summary>
    /// Determines if this factory can handle the specified resource type.
    /// </summary>
    /// <param name="resourceType">The Former2 resource type (e.g., "ec2.vpc").</param>
    /// <returns>True if this factory can create a model for the resource type.</returns>
    bool CanHandle(string resourceType);
}
