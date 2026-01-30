namespace Infragraph.Common.Models.Domain;

/// <summary>
/// VPC resource model.
/// </summary>
public sealed class VpcResource : AwsResource
{
    public string? VpcId { get; init; }
    public string? CidrBlock { get; init; }
    public bool IsDefault { get; init; }
    public bool EnableDnsSupport { get; init; }
    public bool EnableDnsHostnames { get; init; }
    public string? State { get; init; }

    public override string DisplayName => Name ?? VpcId ?? base.DisplayName;
}

/// <summary>
/// Subnet resource model.
/// </summary>
public sealed class SubnetResource : AwsResource
{
    public string? SubnetId { get; init; }
    public required string VpcId { get; init; }
    public string? CidrBlock { get; init; }
    public string? AvailabilityZone { get; init; }
    public bool MapPublicIpOnLaunch { get; init; }
    public string? State { get; init; }

    public override string DisplayName => Name ?? SubnetId ?? base.DisplayName;
}

/// <summary>
/// Security Group resource model.
/// </summary>
public sealed class SecurityGroupResource : AwsResource
{
    public string? GroupId { get; init; }
    public string? GroupName { get; init; }
    public required string VpcId { get; init; }
    public string? Description { get; init; }
    public List<SecurityGroupRule> IngressRules { get; init; } = [];
    public List<SecurityGroupRule> EgressRules { get; init; } = [];
    public List<string> ReferencedSecurityGroups { get; init; } = [];

    public override string DisplayName => Name ?? GroupName ?? GroupId ?? base.DisplayName;
}

/// <summary>
/// Security group rule.
/// </summary>
public sealed class SecurityGroupRule
{
    public string? Protocol { get; init; }
    public int? FromPort { get; init; }
    public int? ToPort { get; init; }
    public List<string> CidrBlocks { get; init; } = [];
    public List<string> SecurityGroupIds { get; init; } = [];
    public string? Description { get; init; }
}

/// <summary>
/// Route Table resource model.
/// </summary>
public sealed class RouteTableResource : AwsResource
{
    public string? RouteTableId { get; init; }
    public required string VpcId { get; init; }
    public List<RouteEntry> Routes { get; init; } = [];
    public List<string> AssociatedSubnetIds { get; init; } = [];

    public override string DisplayName => Name ?? RouteTableId ?? base.DisplayName;
}

/// <summary>
/// Route entry in a route table.
/// </summary>
public sealed class RouteEntry
{
    public string? DestinationCidrBlock { get; init; }
    public string? GatewayId { get; init; }
    public string? NatGatewayId { get; init; }
    public string? TransitGatewayId { get; init; }
    public string? VpcEndpointId { get; init; }
}

/// <summary>
/// Internet Gateway resource model.
/// </summary>
public sealed class InternetGatewayResource : AwsResource
{
    public string? InternetGatewayId { get; init; }
    public List<string> AttachedVpcIds { get; init; } = [];

    public override string DisplayName => Name ?? InternetGatewayId ?? base.DisplayName;
}

/// <summary>
/// NAT Gateway resource model.
/// </summary>
public sealed class NatGatewayResource : AwsResource
{
    public string? NatGatewayId { get; init; }
    public string? SubnetId { get; init; }
    public string? VpcId { get; init; }
    public string? State { get; init; }

    public override string DisplayName => Name ?? NatGatewayId ?? base.DisplayName;
}

/// <summary>
/// Transit Gateway resource model.
/// </summary>
public sealed class TransitGatewayResource : AwsResource
{
    public string? TransitGatewayId { get; init; }
    public string? State { get; init; }
    public List<string> AttachedVpcIds { get; init; } = [];

    public override string DisplayName => Name ?? TransitGatewayId ?? base.DisplayName;
}
