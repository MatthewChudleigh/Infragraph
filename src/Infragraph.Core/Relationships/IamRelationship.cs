using Infragraph.Common.Abstractions;
using Infragraph.Common.Models.Domain;

namespace Infragraph.Core.Relationships;

/// <summary>
/// Extracts IAM role relationships (policies, instance profiles).
/// </summary>
public sealed class IamRelationship : IRelationshipExtractor
{
    public IEnumerable<string> SupportedResourceTypes =>
        ["iam.role", "iam.instanceprofile", "iam.user"];

    public IEnumerable<ResourceRelationship> ExtractRelationships(
        IReadOnlyDictionary<string, AwsResource> index,
        AwsResource source)
    {
        switch (source)
        {
            case IamRoleResource role:
                foreach (var rel in ExtractRoleRelationships(role, index))
                    yield return rel;
                break;

            case InstanceProfileResource profile:
                foreach (var rel in ExtractInstanceProfileRelationships(profile, index))
                    yield return rel;
                break;

            case IamUserResource user:
                foreach (var rel in ExtractUserRelationships(user, index))
                    yield return rel;
                break;
        }
    }

    private static IEnumerable<ResourceRelationship> ExtractRoleRelationships(
        IamRoleResource role,
        IReadOnlyDictionary<string, AwsResource> index)
    {
        // Role attached policies
        foreach (var policyArn in role.AttachedPolicyArns)
        {
            var policy = FindResourceByArn(index, policyArn);
            if (policy != null)
            {
                yield return new ResourceRelationship
                {
                    SourceId = role.Id,
                    TargetId = policy.Id,
                    RelationshipType = RelationshipType.Uses,
                    Label = "has policy"
                };
            }
        }
    }

    private static IEnumerable<ResourceRelationship> ExtractInstanceProfileRelationships(
        InstanceProfileResource profile,
        IReadOnlyDictionary<string, AwsResource> index)
    {
        // Instance profile has roles
        foreach (var roleArn in profile.RoleArns)
        {
            var role = FindResourceByArn(index, roleArn);
            if (role != null)
            {
                yield return new ResourceRelationship
                {
                    SourceId = profile.Id,
                    TargetId = role.Id,
                    RelationshipType = RelationshipType.Contains,
                    Label = "contains role"
                };
            }
        }
    }

    private static IEnumerable<ResourceRelationship> ExtractUserRelationships(
        IamUserResource user,
        IReadOnlyDictionary<string, AwsResource> index)
    {
        // User attached policies
        foreach (var policyArn in user.AttachedPolicyArns)
        {
            var policy = FindResourceByArn(index, policyArn);
            if (policy != null)
            {
                yield return new ResourceRelationship
                {
                    SourceId = user.Id,
                    TargetId = policy.Id,
                    RelationshipType = RelationshipType.Uses,
                    Label = "has policy"
                };
            }
        }
    }

    private static AwsResource? FindResourceByArn(
        IReadOnlyDictionary<string, AwsResource> index,
        string? arn)
    {
        if (string.IsNullOrEmpty(arn))
            return null;

        if (index.TryGetValue(arn, out var resource))
            return resource;

        return index.Values.FirstOrDefault(r => r.Arn == arn);
    }
}
