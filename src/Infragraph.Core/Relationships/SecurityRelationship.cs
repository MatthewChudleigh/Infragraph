using Infragraph.Common.Abstractions;
using Infragraph.Common.Models.Domain;

namespace Infragraph.Core.Relationships;

/// <summary>
/// Extracts security group reference relationships.
/// </summary>
public sealed class SecurityRelationship : IRelationshipExtractor
{
    public IEnumerable<string> SupportedResourceTypes => AllRelationships.SecurityResourceTypes;

    public IEnumerable<ResourceRelationship> ExtractRelationships(
        IReadOnlyDictionary<string, AwsResource> index,
        AwsResource source)
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
