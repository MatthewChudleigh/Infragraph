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
public sealed class ResourceModelFactory(IEnumerable<IRelationshipExtractor> extractors) : IResourceModelFactory
{
    private static readonly Dictionary<string, IResourceTypeHandler> Handlers = new(StringComparer.OrdinalIgnoreCase)
    {
        // VPC/Networking
        [SupportedResourceTypes.Vpc] = new VpcHandler(),
        [SupportedResourceTypes.Subnet] = new SubnetHandler(),
        [SupportedResourceTypes.SecurityGroup] = new SecurityGroupHandler(),
        [SupportedResourceTypes.RouteTable] = new RouteTableHandler(),
        [SupportedResourceTypes.InternetGateway] = new InternetGatewayHandler(),
        [SupportedResourceTypes.NatGateway] = new NatGatewayHandler(),
        [SupportedResourceTypes.TransitGateway] = new TransitGatewayHandler(),

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
    public bool CanHandle(string resourceType) => Handlers.ContainsKey(resourceType);

    public ResourceSet CreateResourceSet(ICollection<Former2Resource> former2Resources)
    {
        var awsResources = former2Resources.Select(CreateModel).ToList();
        var resourceIndex = BuildResourceIndex(awsResources);
        var relationships = BuildRelationships(extractors, awsResources, resourceIndex);
        return new ResourceSet()
        {
            Resources = awsResources,
            ResourceIndex = resourceIndex,
            Relationships = relationships,
        };
    }
    
    public AwsResource CreateModel(Former2Resource resource)
    {
        var awsResource = Handlers.TryGetValue(resource.Type, out var handler)
            ? handler.CreateResource(resource)
            // Fall back to generic resource
            : CreateGenericResource(resource);
        
        awsResource.Account = resource.Account;
        
        return awsResource;
    }

    private static List<ResourceRelationship> BuildRelationships(
        IEnumerable<IRelationshipExtractor> extractors,
        ICollection<AwsResource> resources,
        IReadOnlyDictionary<string, AwsResource> resourceIndex)
    {
        var relationships = new List<ResourceRelationship>();
        
        foreach (var extractor in extractors)
        {
            foreach (var resource in resources)
            {
                if (extractor.SupportedResourceTypes.Contains(resource.Type))
                {
                    relationships.AddRange(extractor.ExtractRelationships(resourceIndex, resource));
                }
            }
        }

        return relationships;
    }
    
    /// <summary>
    /// Adds alternative IDs to the resource index for easier lookup.
    /// </summary>
    private static Dictionary<string, AwsResource> BuildResourceIndex(List<AwsResource> resources)
    {
        var index = new Dictionary<string, AwsResource>();
        
        foreach (var resource in resources)
        {
            index.TryAdd(resource.Id, resource);
            
            // Also index by common ID formats (VpcId, SubnetId, etc.)
            
            // Add by ARN if different from ID
            if (!string.IsNullOrEmpty(resource.Arn) && resource.Arn != resource.Id)
            {
                index.TryAdd(resource.Arn, resource);
            }

            // Add by resource-specific IDs
            switch (resource)
            {
                case VpcResource vpc when !string.IsNullOrEmpty(vpc.VpcId):
                    index.TryAdd(vpc.VpcId, resource);
                    break;
                case SubnetResource subnet when !string.IsNullOrEmpty(subnet.SubnetId):
                    index.TryAdd(subnet.SubnetId, resource);
                    break;
                case SecurityGroupResource sg when !string.IsNullOrEmpty(sg.GroupId):
                    index.TryAdd(sg.GroupId, resource);
                    break;
                case RouteTableResource rt when !string.IsNullOrEmpty(rt.RouteTableId):
                    index.TryAdd(rt.RouteTableId, resource);
                    break;
                case InternetGatewayResource igw when !string.IsNullOrEmpty(igw.InternetGatewayId):
                    index.TryAdd(igw.InternetGatewayId, resource);
                    break;
                case NatGatewayResource nat when !string.IsNullOrEmpty(nat.NatGatewayId):
                    index.TryAdd(nat.NatGatewayId, resource);
                    break;
                case TransitGatewayResource tgw when !string.IsNullOrEmpty(tgw.TransitGatewayId):
                    index.TryAdd(tgw.TransitGatewayId, resource);
                    break;
                case Ec2InstanceResource ec2 when !string.IsNullOrEmpty(ec2.InstanceId):
                    index.TryAdd(ec2.InstanceId, resource);
                    break;
                case EcsClusterResource cluster when !string.IsNullOrEmpty(cluster.ClusterArn):
                    index.TryAdd(cluster.ClusterArn, resource);
                    break;
                case EcsServiceResource svc when !string.IsNullOrEmpty(svc.ServiceArn):
                    index.TryAdd(svc.ServiceArn, resource);
                    break;
                case EcsTaskDefinitionResource td when !string.IsNullOrEmpty(td.TaskDefinitionArn):
                    index.TryAdd(td.TaskDefinitionArn, resource);
                    break;
                case LoadBalancerResource lb when !string.IsNullOrEmpty(lb.LoadBalancerArn):
                    index.TryAdd(lb.LoadBalancerArn, resource);
                    break;
                case TargetGroupResource tg when !string.IsNullOrEmpty(tg.TargetGroupArn):
                    index.TryAdd(tg.TargetGroupArn, resource);
                    break;
                case ListenerResource listener when !string.IsNullOrEmpty(listener.ListenerArn):
                    index.TryAdd(listener.ListenerArn, resource);
                    break;
                case IamRoleResource role when !string.IsNullOrEmpty(role.RoleArn):
                    index.TryAdd(role.RoleArn, resource);
                    break;
                case InstanceProfileResource profile when !string.IsNullOrEmpty(profile.InstanceProfileArn):
                    index.TryAdd(profile.InstanceProfileArn, resource);
                    break;
                case IamPolicyResource policy when !string.IsNullOrEmpty(policy.PolicyArn):
                    index.TryAdd(policy.PolicyArn, resource);
                    break;
                case LambdaFunctionResource lambda when !string.IsNullOrEmpty(lambda.FunctionArn):
                    index.TryAdd(lambda.FunctionArn, resource);
                    break;
            }
        }

        return index;
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
