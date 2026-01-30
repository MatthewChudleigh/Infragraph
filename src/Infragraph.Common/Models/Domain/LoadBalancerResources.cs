namespace Infragraph.Common.Models.Domain;

/// <summary>
/// Elastic Load Balancer resource model.
/// </summary>
public sealed class LoadBalancerResource : AwsResource
{
    public string? LoadBalancerArn { get; init; }
    public string? LoadBalancerName { get; init; }
    public string? DnsName { get; init; }
    public required string VpcId { get; init; }
    public string? Scheme { get; init; }
    public string? LoadBalancerType { get; init; }
    public string? State { get; init; }
    public List<string> SubnetIds { get; init; } = [];
    public List<string> SecurityGroupIds { get; init; } = [];
    public List<string> AvailabilityZones { get; init; } = [];

    public override string DisplayName => Name ?? LoadBalancerName ?? base.DisplayName;
}

/// <summary>
/// Target Group resource model.
/// </summary>
public sealed class TargetGroupResource : AwsResource
{
    public string? TargetGroupArn { get; init; }
    public string? TargetGroupName { get; init; }
    public string? VpcId { get; init; }
    public string? Protocol { get; init; }
    public int? Port { get; init; }
    public string? TargetType { get; init; }
    public string? HealthCheckPath { get; init; }
    public List<string> LoadBalancerArns { get; init; } = [];

    public override string DisplayName => Name ?? TargetGroupName ?? base.DisplayName;
}

/// <summary>
/// Load Balancer Listener resource model.
/// </summary>
public sealed class ListenerResource : AwsResource
{
    public string? ListenerArn { get; init; }
    public required string LoadBalancerArn { get; init; }
    public int? Port { get; init; }
    public string? Protocol { get; init; }
    public List<ListenerAction> DefaultActions { get; init; } = [];

    public override string DisplayName => $"{Protocol}:{Port}";
}

/// <summary>
/// Listener action.
/// </summary>
public sealed class ListenerAction
{
    public string? Type { get; init; }
    public string? TargetGroupArn { get; init; }
    public int? Order { get; init; }
}
