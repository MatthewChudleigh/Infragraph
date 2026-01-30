namespace Infragraph.Common.Models.Domain;

using System.Text.Json;

/// <summary>
/// Base class for all AWS resource domain models.
/// </summary>
public abstract class AwsResource
{
    /// <summary>
    /// The resource identifier (typically the ARN or unique ID).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The Amazon Resource Name (ARN) if available.
    /// </summary>
    public string? Arn { get; init; }

    /// <summary>
    /// The Former2 resource type (e.g., "ec2.vpc").
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// The AWS region.
    /// </summary>
    public string? Region { get; init; }

    /// <summary>
    /// The resource name (from Name tag or resource-specific field).
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Resource tags.
    /// </summary>
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// The raw data for accessing additional properties.
    /// </summary>
    public JsonElement RawData { get; init; }

    /// <summary>
    /// Gets a display name for the resource.
    /// </summary>
    public virtual string DisplayName => Name ?? GetShortId();

    /// <summary>
    /// Gets a short identifier for display purposes.
    /// </summary>
    protected virtual string GetShortId()
    {
        // For ARNs, return the last segment
        if (Id.StartsWith("arn:"))
        {
            var segments = Id.Split(':');
            return segments.Length > 0 ? segments[^1].Split('/')[^1] : Id;
        }
        return Id;
    }

    /// <summary>
    /// Gets the AWS service name (e.g., "ec2", "iam").
    /// </summary>
    public string ServiceName => Type.Split('.')[0];

    /// <summary>
    /// Gets the resource type name (e.g., "vpc", "role").
    /// </summary>
    public string ResourceTypeName => Type.Contains('.') ? Type.Split('.')[1] : Type;
}
