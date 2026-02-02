namespace Infragraph.Core.Relationships.Extractors;

using Common.Abstractions;
using Common.Models.Domain;

/// <summary>
/// Extracts ECS service relationships (cluster, task definition, subnets, security groups, load balancers).
/// </summary>
public sealed class EcsServiceExtractor : IRelationshipExtractor
{
    public IEnumerable<string> SupportedResourceTypes => ["ecs.service", "ecs.cluster", "ecs.taskdefinition"];

    public IEnumerable<ResourceRelationship> ExtractRelationships(
        AwsResource source,
        IReadOnlyDictionary<string, AwsResource> index)
    {
        switch (source)
        {
            case EcsServiceResource service:
                foreach (var rel in ExtractServiceRelationships(service, index))
                    yield return rel;
                break;

            case EcsTaskDefinitionResource taskDef:
                foreach (var rel in ExtractTaskDefinitionRelationships(taskDef, index))
                    yield return rel;
                break;
        }
    }

    private static IEnumerable<ResourceRelationship> ExtractServiceRelationships(
        EcsServiceResource service,
        IReadOnlyDictionary<string, AwsResource> index)
    {
        // Service belongs to cluster
        var cluster = FindResourceByArn(index, service.ClusterArn);
        if (cluster != null)
        {
            yield return new ResourceRelationship
            {
                SourceId = service.Id,
                TargetId = cluster.Id,
                RelationshipType = RelationshipType.BelongsTo,
                Label = "in cluster"
            };

            // Cluster contains service
            yield return new ResourceRelationship
            {
                SourceId = cluster.Id,
                TargetId = service.Id,
                RelationshipType = RelationshipType.Contains,
                Label = "contains"
            };
        }

        // Service uses task definition
        if (!string.IsNullOrEmpty(service.TaskDefinitionArn))
        {
            var taskDef = FindResourceByArn(index, service.TaskDefinitionArn);
            if (taskDef != null)
            {
                yield return new ResourceRelationship
                {
                    SourceId = service.Id,
                    TargetId = taskDef.Id,
                    RelationshipType = RelationshipType.Uses,
                    Label = "uses"
                };
            }
        }

        // Service uses subnets
        foreach (var subnetId in service.SubnetIds)
        {
            if (index.TryGetValue(subnetId, out var subnet))
            {
                yield return new ResourceRelationship
                {
                    SourceId = service.Id,
                    TargetId = subnet.Id,
                    RelationshipType = RelationshipType.Uses,
                    Label = "runs in"
                };
            }
        }

        // Service uses security groups
        foreach (var sgId in service.SecurityGroupIds)
        {
            if (index.TryGetValue(sgId, out var sg))
            {
                yield return new ResourceRelationship
                {
                    SourceId = service.Id,
                    TargetId = sg.Id,
                    RelationshipType = RelationshipType.Uses,
                    Label = "uses"
                };
            }
        }

        // Service attached to target groups
        foreach (var lb in service.LoadBalancers)
        {
            if (!string.IsNullOrEmpty(lb.TargetGroupArn))
            {
                var tg = FindResourceByArn(index, lb.TargetGroupArn);
                if (tg != null)
                {
                    yield return new ResourceRelationship
                    {
                        SourceId = service.Id,
                        TargetId = tg.Id,
                        RelationshipType = RelationshipType.AttachedTo,
                        Label = $":{lb.ContainerPort}"
                    };
                }
            }
        }

        // Service assumes role
        if (!string.IsNullOrEmpty(service.RoleArn))
        {
            var role = FindResourceByArn(index, service.RoleArn);
            if (role != null)
            {
                yield return new ResourceRelationship
                {
                    SourceId = service.Id,
                    TargetId = role.Id,
                    RelationshipType = RelationshipType.Assumes,
                    Label = "assumes"
                };
            }
        }
    }

    private static IEnumerable<ResourceRelationship> ExtractTaskDefinitionRelationships(
        EcsTaskDefinitionResource taskDef,
        IReadOnlyDictionary<string, AwsResource> index)
    {
        // Task role
        if (!string.IsNullOrEmpty(taskDef.TaskRoleArn))
        {
            var role = FindResourceByArn(index, taskDef.TaskRoleArn);
            if (role != null)
            {
                yield return new ResourceRelationship
                {
                    SourceId = taskDef.Id,
                    TargetId = role.Id,
                    RelationshipType = RelationshipType.Assumes,
                    Label = "task role"
                };
            }
        }

        // Execution role
        if (!string.IsNullOrEmpty(taskDef.ExecutionRoleArn))
        {
            var role = FindResourceByArn(index, taskDef.ExecutionRoleArn);
            if (role != null)
            {
                yield return new ResourceRelationship
                {
                    SourceId = taskDef.Id,
                    TargetId = role.Id,
                    RelationshipType = RelationshipType.Assumes,
                    Label = "execution role"
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

        // Try direct match by ARN as ID
        if (index.TryGetValue(arn, out var resource))
            return resource;

        // Try matching by Arn property
        return index.Values.FirstOrDefault(r => r.Arn == arn);
    }
}
