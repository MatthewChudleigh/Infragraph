using Infragraph.Common.Abstractions;
using Infragraph.Common.Models.Domain;

namespace Infragraph.Core.Relationships;

/// <summary>
/// Extracts EC2 instance and Lambda function relationships.
/// </summary>
public sealed class ComputeExtractor : IRelationshipExtractor
{
    public IEnumerable<string> SupportedResourceTypes =>
        ["ec2.instance", "ec2.volume", "lambda.function"];

    public IEnumerable<ResourceRelationship> ExtractRelationships(
        IReadOnlyDictionary<string, AwsResource> index,
        AwsResource source)
    {
        switch (source)
        {
            case Ec2InstanceResource instance:
                foreach (var rel in ExtractInstanceRelationships(instance, index))
                    yield return rel;
                break;

            case EbsVolumeResource volume:
                foreach (var rel in ExtractVolumeRelationships(volume, index))
                    yield return rel;
                break;

            case LambdaFunctionResource lambda:
                foreach (var rel in ExtractLambdaRelationships(lambda, index))
                    yield return rel;
                break;
        }
    }

    private static IEnumerable<ResourceRelationship> ExtractInstanceRelationships(
        Ec2InstanceResource instance,
        IReadOnlyDictionary<string, AwsResource> index)
    {
        // Instance in VPC
        if (!string.IsNullOrEmpty(instance.VpcId))
        {
            var vpc = FindResourceByProperty<VpcResource>(index, r => r.VpcId == instance.VpcId || r.Id == instance.VpcId);
            if (vpc != null)
            {
                yield return new ResourceRelationship
                {
                    SourceId = instance.Id,
                    TargetId = vpc.Id,
                    RelationshipType = RelationshipType.BelongsTo,
                    Label = "in VPC"
                };
            }
        }

        // Instance in subnet
        if (!string.IsNullOrEmpty(instance.SubnetId))
        {
            if (index.TryGetValue(instance.SubnetId, out var subnet))
            {
                yield return new ResourceRelationship
                {
                    SourceId = instance.Id,
                    TargetId = subnet.Id,
                    RelationshipType = RelationshipType.BelongsTo,
                    Label = "in subnet"
                };
            }
        }

        // Instance uses security groups
        foreach (var sgId in instance.SecurityGroupIds)
        {
            if (index.TryGetValue(sgId, out var sg))
            {
                yield return new ResourceRelationship
                {
                    SourceId = instance.Id,
                    TargetId = sg.Id,
                    RelationshipType = RelationshipType.Uses,
                    Label = "uses"
                };
            }
        }

        // Instance uses instance profile
        if (!string.IsNullOrEmpty(instance.IamInstanceProfileArn))
        {
            var profile = FindResourceByArn(index, instance.IamInstanceProfileArn);
            if (profile != null)
            {
                yield return new ResourceRelationship
                {
                    SourceId = instance.Id,
                    TargetId = profile.Id,
                    RelationshipType = RelationshipType.Uses,
                    Label = "uses"
                };
            }
        }
    }

    private static IEnumerable<ResourceRelationship> ExtractVolumeRelationships(
        EbsVolumeResource volume,
        IReadOnlyDictionary<string, AwsResource> index)
    {
        // Volume attached to instance(s)
        foreach (var attachment in volume.Attachments)
        {
            if (string.IsNullOrEmpty(attachment.InstanceId))
                continue;

            // Find instance by InstanceId property
            var instance = FindResourceByProperty<Ec2InstanceResource>(
                index, i => i.InstanceId == attachment.InstanceId || i.Id == attachment.InstanceId);

            if (instance != null)
            {
                yield return new ResourceRelationship
                {
                    SourceId = volume.Id,
                    TargetId = instance.Id,
                    RelationshipType = RelationshipType.AttachedTo,
                    Label = attachment.Device ?? "attached"
                };
            }
        }
    }

    private static IEnumerable<ResourceRelationship> ExtractLambdaRelationships(
        LambdaFunctionResource lambda,
        IReadOnlyDictionary<string, AwsResource> index)
    {
        // Lambda in VPC
        if (!string.IsNullOrEmpty(lambda.VpcId))
        {
            var vpc = FindResourceByProperty<VpcResource>(index, r => r.VpcId == lambda.VpcId || r.Id == lambda.VpcId);
            if (vpc != null)
            {
                yield return new ResourceRelationship
                {
                    SourceId = lambda.Id,
                    TargetId = vpc.Id,
                    RelationshipType = RelationshipType.BelongsTo,
                    Label = "in VPC"
                };
            }
        }

        // Lambda uses subnets
        foreach (var subnetId in lambda.SubnetIds)
        {
            if (index.TryGetValue(subnetId, out var subnet))
            {
                yield return new ResourceRelationship
                {
                    SourceId = lambda.Id,
                    TargetId = subnet.Id,
                    RelationshipType = RelationshipType.Uses,
                    Label = "uses"
                };
            }
        }

        // Lambda uses security groups
        foreach (var sgId in lambda.SecurityGroupIds)
        {
            if (index.TryGetValue(sgId, out var sg))
            {
                yield return new ResourceRelationship
                {
                    SourceId = lambda.Id,
                    TargetId = sg.Id,
                    RelationshipType = RelationshipType.Uses,
                    Label = "uses"
                };
            }
        }

        // Lambda assumes role
        if (!string.IsNullOrEmpty(lambda.RoleArn))
        {
            var role = FindResourceByArn(index, lambda.RoleArn);
            if (role != null)
            {
                yield return new ResourceRelationship
                {
                    SourceId = lambda.Id,
                    TargetId = role.Id,
                    RelationshipType = RelationshipType.Assumes,
                    Label = "assumes"
                };
            }
        }
    }

    private static T? FindResourceByProperty<T>(
        IReadOnlyDictionary<string, AwsResource> index,
        Func<T, bool> predicate) where T : AwsResource
    {
        return index.Values.OfType<T>().FirstOrDefault(predicate);
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
