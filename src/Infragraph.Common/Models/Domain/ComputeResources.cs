namespace Infragraph.Common.Models.Domain;

/// <summary>
/// EC2 Instance resource model.
/// </summary>
public sealed class Ec2InstanceResource : AwsResource
{
    public string? InstanceId { get; init; }
    public string? InstanceType { get; init; }
    public string? SubnetId { get; init; }
    public string? VpcId { get; init; }
    public string? State { get; init; }
    public string? PrivateIpAddress { get; init; }
    public string? PublicIpAddress { get; init; }
    public List<string> SecurityGroupIds { get; init; } = [];
    public string? IamInstanceProfileArn { get; init; }

    public override string DisplayName => Name ?? InstanceId ?? base.DisplayName;
}

/// <summary>
/// EBS Volume resource model.
/// </summary>
public sealed class EbsVolumeResource : AwsResource
{
    public string? VolumeId { get; init; }
    public string? AvailabilityZone { get; init; }
    public string? VolumeType { get; init; }
    public int? Size { get; init; }
    public string? State { get; init; }
    public bool Encrypted { get; init; }
    public List<VolumeAttachment> Attachments { get; init; } = [];

    public override string DisplayName => Name ?? VolumeId ?? base.DisplayName;
}

/// <summary>
/// EBS volume attachment to an instance.
/// </summary>
public sealed class VolumeAttachment
{
    public string? InstanceId { get; init; }
    public string? Device { get; init; }
    public string? State { get; init; }
}

/// <summary>
/// ECS Cluster resource model.
/// </summary>
public sealed class EcsClusterResource : AwsResource
{
    public string? ClusterArn { get; init; }
    public string? ClusterName { get; init; }
    public string? Status { get; init; }
    public int RunningTasksCount { get; init; }
    public int ActiveServicesCount { get; init; }

    public override string DisplayName => Name ?? ClusterName ?? base.DisplayName;
}

/// <summary>
/// ECS Service resource model.
/// </summary>
public sealed class EcsServiceResource : AwsResource
{
    public string? ServiceArn { get; init; }
    public string? EcsServiceName { get; init; }
    public required string ClusterArn { get; init; }
    public string? TaskDefinitionArn { get; init; }
    public string? Status { get; init; }
    public int DesiredCount { get; init; }
    public int RunningCount { get; init; }
    public string? LaunchType { get; init; }
    public List<string> SubnetIds { get; init; } = [];
    public List<string> SecurityGroupIds { get; init; } = [];
    public List<LoadBalancerAttachment> LoadBalancers { get; init; } = [];
    public string? RoleArn { get; init; }

    public override string DisplayName => Name ?? EcsServiceName ?? base.DisplayName;
}

/// <summary>
/// Load balancer attachment for ECS service.
/// </summary>
public sealed class LoadBalancerAttachment
{
    public string? TargetGroupArn { get; init; }
    public string? ContainerName { get; init; }
    public int? ContainerPort { get; init; }
}

/// <summary>
/// ECS Task Definition resource model.
/// </summary>
public sealed class EcsTaskDefinitionResource : AwsResource
{
    public string? TaskDefinitionArn { get; init; }
    public string? Family { get; init; }
    public int Revision { get; init; }
    public string? TaskRoleArn { get; init; }
    public string? ExecutionRoleArn { get; init; }
    public string? NetworkMode { get; init; }
    public List<string> RequiresCompatibilities { get; init; } = [];
    public int? Cpu { get; init; }
    public int? Memory { get; init; }
    public List<ContainerDefinition> Containers { get; init; } = [];

    public override string DisplayName => Name ?? Family ?? base.DisplayName;
}

/// <summary>
/// Container definition in a task definition.
/// </summary>
public sealed class ContainerDefinition
{
    public string? Name { get; init; }
    public string? Image { get; init; }
    public int? Cpu { get; init; }
    public int? Memory { get; init; }
    public List<PortMapping> PortMappings { get; init; } = [];
}

/// <summary>
/// Port mapping in a container definition.
/// </summary>
public sealed class PortMapping
{
    public int? ContainerPort { get; init; }
    public int? HostPort { get; init; }
    public string? Protocol { get; init; }
}

/// <summary>
/// Lambda Function resource model.
/// </summary>
public sealed class LambdaFunctionResource : AwsResource
{
    public string? FunctionArn { get; init; }
    public string? FunctionName { get; init; }
    public string? Runtime { get; init; }
    public string? Handler { get; init; }
    public string? RoleArn { get; init; }
    public int? Timeout { get; init; }
    public int? MemorySize { get; init; }
    public string? VpcId { get; init; }
    public List<string> SubnetIds { get; init; } = [];
    public List<string> SecurityGroupIds { get; init; } = [];

    public override string DisplayName => Name ?? FunctionName ?? base.DisplayName;
}
