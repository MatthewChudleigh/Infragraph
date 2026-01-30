namespace Infragraph.Common.Models.Domain;

/// <summary>
/// S3 Bucket resource model.
/// </summary>
public sealed class S3BucketResource : AwsResource
{
    public string? BucketName { get; init; }
    public string? BucketArn { get; init; }
    public DateTimeOffset? CreationDate { get; init; }

    public override string DisplayName => Name ?? BucketName ?? base.DisplayName;
}

/// <summary>
/// RDS Instance resource model.
/// </summary>
public sealed class RdsInstanceResource : AwsResource
{
    public string? DbInstanceIdentifier { get; init; }
    public string? DbInstanceArn { get; init; }
    public string? Engine { get; init; }
    public string? EngineVersion { get; init; }
    public string? DbInstanceClass { get; init; }
    public string? VpcId { get; init; }
    public List<string> SubnetIds { get; init; } = [];
    public List<string> SecurityGroupIds { get; init; } = [];
    public string? Status { get; init; }

    public override string DisplayName => Name ?? DbInstanceIdentifier ?? base.DisplayName;
}

/// <summary>
/// DynamoDB Table resource model.
/// </summary>
public sealed class DynamoDbTableResource : AwsResource
{
    public string? TableName { get; init; }
    public string? TableArn { get; init; }
    public string? TableStatus { get; init; }
    public long? ItemCount { get; init; }
    public long? TableSizeBytes { get; init; }

    public override string DisplayName => Name ?? TableName ?? base.DisplayName;
}

/// <summary>
/// SQS Queue resource model.
/// </summary>
public sealed class SqsQueueResource : AwsResource
{
    public string? QueueUrl { get; init; }
    public string? QueueArn { get; init; }
    public string? QueueName { get; init; }
    public bool IsFifo { get; init; }

    public override string DisplayName => Name ?? QueueName ?? base.DisplayName;
}

/// <summary>
/// SNS Topic resource model.
/// </summary>
public sealed class SnsTopicResource : AwsResource
{
    public string? TopicArn { get; init; }
    public string? TopicName { get; init; }

    public override string DisplayName => Name ?? TopicName ?? base.DisplayName;
}
