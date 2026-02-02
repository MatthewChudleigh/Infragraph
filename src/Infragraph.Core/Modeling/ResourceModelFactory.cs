using Infragraph.Common.Configuration;

namespace Infragraph.Core.Modeling;

using System.Text.Json;
using Common.Abstractions;
using Common.Models.Domain;
using Common.Models.Former2;
using ResourceTypes;

/// <summary>
/// Interface for resource type-specific handling.
/// </summary>
internal interface IResourceTypeHandler
{
    AwsResource CreateResource(Former2Resource resource);
}

/// <summary>
/// Factory for creating typed AWS resource models.
/// </summary>
public sealed class ResourceModelFactory : IResourceModelFactory
{
    private readonly Dictionary<string, IResourceTypeHandler> _handlers = new(StringComparer.OrdinalIgnoreCase)
    {
        // VPC/Networking
        ["ec2.vpc"] = new VpcHandler(),
        ["ec2.subnet"] = new SubnetHandler(),
        ["ec2.securitygroup"] = new SecurityGroupHandler(),
        ["ec2.routetable"] = new RouteTableHandler(),
        ["ec2.internetgateway"] = new InternetGatewayHandler(),
        ["ec2.natgateway"] = new NatGatewayHandler(),
        ["ec2.transitgateway"] = new TransitGatewayHandler(),

        // Compute
        ["ec2.instance"] = new Ec2InstanceHandler(),
        ["ec2.volume"] = new EbsVolumeHandler(),
        [SupportedResourceTypes.EcsCluster] = new EcsClusterHandler(),
        [SupportedResourceTypes.EcsService] = new EcsServiceHandler(),
        [SupportedResourceTypes.EcsTaskDefinition] = new EcsTaskDefinitionHandler(),

        // Load Balancing
        [SupportedResourceTypes.LoadBalancer] = new LoadBalancerHandler(),
        ["elbv2.targetgroup"] = new TargetGroupHandler(),
        ["elbv2.listener"] = new ListenerHandler(),
        ["elbv2.loadbalancerlistener"] = new ListenerHandler(),

        // IAM
        ["iam.role"] = new IamRoleHandler(),
        ["iam.user"] = new IamUserHandler(),
        ["iam.policy"] = new IamPolicyHandler(),
        ["iam.instanceprofile"] = new InstanceProfileHandler(),

        // Storage
        ["s3.bucket"] = new S3BucketHandler(),
        ["rds.dbinstance"] = new RdsInstanceHandler(),
        ["dynamodb.table"] = new DynamoDbTableHandler(),

        // Other
        ["lambda.function"] = new LambdaFunctionHandler(),
        ["sqs.queue"] = new SqsQueueHandler(),
        ["sns.topic"] = new SnsTopicHandler(),
    };

    // VPC/Networking
    // Compute
    // Load Balancing
    // IAM
    // Storage
    // Other

    /// <inheritdoc />
    public bool CanHandle(string resourceType) => _handlers.ContainsKey(resourceType);

    /// <inheritdoc />
    public AwsResource CreateModel(Former2Resource resource)
    {
        return _handlers.TryGetValue(resource.Type, out var handler)
            ? handler.CreateResource(resource)
            // Fall back to generic resource
            : CreateGenericResource(resource);
    }

    private static GenericAwsResource CreateGenericResource(Former2Resource resource)
    {
        var tags = ExtractTags(resource.Data);
        var name = tags.GetValueOrDefault("Name") ?? ExtractName(resource.Data) ?? GetShortId(resource.Id);

        return new GenericAwsResource
        {
            Id = resource.Id,
            Arn = ExtractArn(resource),
            Type = resource.Type,
            Region = resource.Region,
            Name = name,
            Tags = tags,
            RawData = resource.Data
        };
    }

    internal static Dictionary<string, string> ExtractTags(JsonElement data)
    {
        var tags = new Dictionary<string, string>();
        if (data.ValueKind != JsonValueKind.Object 
            || !data.TryGetProperty("Tags", out var tagsElement) 
            || tagsElement.ValueKind != JsonValueKind.Array) return tags;

        foreach (var tag in tagsElement.EnumerateArray())
        {
            var key = tag.TryGetProperty("Key", out var k) ? k.GetString()
                : tag.TryGetProperty("key", out var k2) ? k2.GetString()
                : null;
            var value = tag.TryGetProperty("Value", out var v) ? v.GetString()
                : tag.TryGetProperty("value", out var v2) ? v2.GetString()
                : null;

            if (!string.IsNullOrEmpty(key))
            {
                tags[key] = value ?? "";
            }
        }

        return tags;
    }

    private static string? ExtractName(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object) return null;

        // Try various name properties
        string[] nameProps = ["Name", "name", "RoleName", "UserName", "BucketName",
            "FunctionName", "QueueName", "TopicName", "serviceName", "clusterName",
            "LoadBalancerName", "GroupName", "DBInstanceIdentifier", "TableName"];

        foreach (var prop in nameProps)
        {
            if (data.TryGetProperty(prop, out var nameElement) &&
                nameElement.ValueKind == JsonValueKind.String)
            {
                return nameElement.GetString();
            }
        }

        return null;
    }

    private static string? ExtractArn(Former2Resource resource)
    {
        if (resource.Id.StartsWith("arn:"))
            return resource.Id;
        if (resource.Data.ValueKind != JsonValueKind.Object) 
            return null;
        
        string[] arnProps = ["Arn", "arn", "serviceArn", "clusterArn", "RoleArn",
            "LoadBalancerArn", "TargetGroupArn", "FunctionArn", "SubnetArn"];

        foreach (var prop in arnProps)
        {
            if (resource.Data.TryGetProperty(prop, out var arnElement) &&
                arnElement.ValueKind == JsonValueKind.String)
            {
                return arnElement.GetString();
            }
        }

        return null;
    }

    private static string GetShortId(string id)
    {
        if (!id.StartsWith("arn:")) return id;
        var segments = id.Split(':');
        return segments.Length > 0 ? segments[^1].Split('/')[^1] : id;
    }
}
