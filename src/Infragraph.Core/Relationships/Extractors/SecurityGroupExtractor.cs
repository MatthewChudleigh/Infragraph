namespace Infragraph.Core.Relationships.Extractors;

using Infragraph.Common.Abstractions;
using Infragraph.Common.Models.Domain;

/// <summary>
/// Extracts security group reference relationships.
/// </summary>
public sealed class SecurityGroupExtractor : IRelationshipExtractor
{
    public IEnumerable<string> SupportedResourceTypes => ["ec2.securitygroup"];

    public IEnumerable<ResourceRelationship> ExtractRelationships(
        AwsResource source,
        IReadOnlyDictionary<string, AwsResource> index)
    {
        if (source is not SecurityGroupResource sg)
            yield break;

        // Security group references other security groups in rules
        foreach (var referencedSgId in sg.ReferencedSecurityGroups)
        {
            if (index.TryGetValue(referencedSgId, out var targetSg))
            {
                yield return new ResourceRelationship
                {
                    SourceId = sg.Id,
                    TargetId = targetSg.Id,
                    RelationshipType = RelationshipType.References,
                    Label = "allows traffic from"
                };
            }
        }
    }
}
