namespace Infragraph.Core.Modeling.ResourceTypes;

using System.Text.Json;
using Common.Models.Domain;
using Common.Models.Former2;

internal sealed class S3BucketHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);

        var bucketName = GetString(data, "Name") ?? GetString(data, "BucketName") ?? resource.Id;

        return new S3BucketResource
        {
            Id = resource.Id,
            Arn = $"arn:aws:s3:::{bucketName}",
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name") ?? bucketName,
            Tags = tags,
            RawData = data,
            BucketName = bucketName
        };
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;
}

internal sealed class RdsInstanceHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);

        var subnetIds = ExtractSubnetIds(data);
        var securityGroups = ExtractSecurityGroups(data);

        return new RdsInstanceResource
        {
            Id = resource.Id,
            Arn = GetString(data, "DBInstanceArn") ?? resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name") ?? GetString(data, "DBInstanceIdentifier"),
            Tags = tags,
            RawData = data,
            DbInstanceIdentifier = GetString(data, "DBInstanceIdentifier"),
            DbInstanceArn = GetString(data, "DBInstanceArn"),
            Engine = GetString(data, "Engine"),
            EngineVersion = GetString(data, "EngineVersion"),
            DbInstanceClass = GetString(data, "DBInstanceClass"),
            VpcId = GetNestedString(data, "DBSubnetGroup", "VpcId"),
            SubnetIds = subnetIds,
            SecurityGroupIds = securityGroups,
            Status = GetString(data, "DBInstanceStatus")
        };
    }

    private static List<string> ExtractSecurityGroups(JsonElement data)
    {
        var securityGroups = new List<string>();
        if (!data.TryGetProperty("VpcSecurityGroups", out var sgs) || sgs.ValueKind != JsonValueKind.Array)
            return securityGroups;
        
        foreach (var sg in sgs.EnumerateArray())
        {
            if (sg.TryGetProperty("VpcSecurityGroupId", out var sgid))
                securityGroups.Add(sgid.GetString() ?? "");
        }

        return securityGroups;
    }

    private static List<string> ExtractSubnetIds(JsonElement data)
    {
        var subnetIds = new List<string>();

        if (!data.TryGetProperty("DBSubnetGroup", out var subnetGroup) 
            || !subnetGroup.TryGetProperty("Subnets", out var subs) 
            || subs.ValueKind != JsonValueKind.Array) 
            return subnetIds;

        foreach (var s in subs.EnumerateArray())
        {
            if (s.TryGetProperty("SubnetIdentifier", out var sid))
                subnetIds.Add(sid.GetString() ?? "");
        }

        return subnetIds;
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;

    private static string? GetNestedString(JsonElement data, string prop, string nested) =>
        data.TryGetProperty(prop, out var obj) && obj.TryGetProperty(nested, out var val) && val.ValueKind == JsonValueKind.String
            ? val.GetString() : null;
}

internal sealed class DynamoDbTableHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);

        return new DynamoDbTableResource
        {
            Id = resource.Id,
            Arn = GetString(data, "TableArn") ?? resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name") ?? GetString(data, "TableName"),
            Tags = tags,
            RawData = data,
            TableName = GetString(data, "TableName"),
            TableArn = GetString(data, "TableArn"),
            TableStatus = GetString(data, "TableStatus"),
            ItemCount = GetLongNullable(data, "ItemCount"),
            TableSizeBytes = GetLongNullable(data, "TableSizeBytes")
        };
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;

    private static long? GetLongNullable(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number ? val.GetInt64() : null;
}

internal sealed class SqsQueueHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);

        var queueUrl = GetString(data, "QueueUrl") ?? resource.Id;
        var queueName = queueUrl.Split('/').LastOrDefault() ?? queueUrl;

        return new SqsQueueResource
        {
            Id = resource.Id,
            Arn = GetString(data, "QueueArn") ?? resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name") ?? queueName,
            Tags = tags,
            RawData = data,
            QueueUrl = queueUrl,
            QueueArn = GetString(data, "QueueArn"),
            QueueName = queueName,
            IsFifo = queueName.EndsWith(".fifo")
        };
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;
}

internal sealed class SnsTopicHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);

        var topicArn = GetString(data, "TopicArn") ?? resource.Id;
        var topicName = topicArn.Split(':').LastOrDefault() ?? topicArn;

        return new SnsTopicResource
        {
            Id = resource.Id,
            Arn = topicArn,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name") ?? topicName,
            Tags = tags,
            RawData = data,
            TopicArn = topicArn,
            TopicName = topicName
        };
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;
}
