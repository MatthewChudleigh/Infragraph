namespace Infragraph.Common.Configuration;

/// <summary>
/// Information about a supported AWS resource type.
/// </summary>
public sealed class ResourceTypeInfo
{
    /// <summary>
    /// The Former2 type string (e.g., "ec2.vpc").
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// AWS service category.
    /// </summary>
    public required string Service { get; init; }

    /// <summary>
    /// Category for grouping in UI.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Icon identifier for frontend rendering.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Default color for the node.
    /// </summary>
    public string? Color { get; init; }

    /// <summary>
    /// Whether this type can be a container/group.
    /// </summary>
    public bool CanContain { get; init; }
}

public static class SupportedServiceTypes
{
    public const string Iam = "iam";
    public const string Ec2 = "ec2";
    public const string Ecs = "ecs";
    public const string Lambda = "lambda";
    public const string ElbV2 = "elbv2";
    public const string S3 = "s3";
    public const string Rds = "rds";
    public const string DynamoDb = "dynamodb";
    public const string SecretsManager = "secretsmanager";
    public const string Ssm = "ssm";
    public const string Sqs = "sqs";
    public const string Sns = "sns";
    public const string CloudWatchLogs = "logs";
    public const string ApiGateway = "apigateway";
    public const string ApiGatewayV2 = "apigatewayv2";
}

/// <summary>
/// Registry of supported resource types.
/// </summary>
public static class SupportedResourceTypes
{
    public const string RamResourceShare = "ram.resourceshare"; // TODO

    public const string TransitGateway = "ec2.transitgateway";
    public const string TransitGatewayAttachment = "ec2.transitgatewayattachment"; // TODO
    public const string TransitGatewayRoute = "ec2.transitgatewayroute"; // TODO
    public const string TransitGatewayRouteTable = "ec2.transitgatewayroutetable"; // TODO
    public const string TransitGatewayRouteTableAssociation = "ec2.transitgatewayroutetableassociation"; // TODO
    public const string TransitGatewayRouteTablePropagation = "ec2.transitgatewayroutetablepropogation"; // TODO
    public const string Route = "ec2.route"; // TODO
    public const string RouteTable = "ec2.routetable";
    public const string SubnetRouteTableAssociation = "ec2.subnetroutetableassociation"; // TODO
    public const string InternetGateway = "ec2.internetgateway";
    public const string NatGateway = "ec2.natgateway";
    public const string Subnet = "ec2.subnet";
    public const string Vpc = "ec2.vpc";
    public const string VpcEndpoint = "ec2.vpcendpoint";
    public const string SecurityGroup = "ec2.securitygroup";
    public const string LoadBalancer = "elbv2.loadbalancer";

    public const string EcsCluster = "ecs.cluster";
    public const string EcsService = "ecs.service";
    public const string EcsTaskDefinition = "ecs.taskdefinition";
    
    public const string CloudfrontDistribution = "cloudfront.distribution"; // TODO
    public const string CloudfrontFunction = "cloudfront.function"; // TODO
    public const string CloudfrontOac = "cloudfront.originaccesscontrol"; // TODO
    public const string CloudfrontOai = "cloudfront.originaccessidentity"; // TODO
    
    /// <summary>
    /// All supported resource types.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, ResourceTypeInfo> All = new Dictionary<string, ResourceTypeInfo>
    {
        // VPC/Networking
        [Vpc] = new() { Type = Vpc, DisplayName = "VPC", Service = SupportedServiceTypes.Ec2, Category = "Networking", CanContain = true, Color = "#FF9900" },
        [Subnet] = new() { Type = Subnet, DisplayName = "Subnet", Service = SupportedServiceTypes.Ec2, Category = "Networking", CanContain = true, Color = "#FF9900" },
        [SecurityGroup] = new() { Type = SecurityGroup, DisplayName = "Security Group", Service = SupportedServiceTypes.Ec2, Category = "Networking", Color = "#DD4B39" },
        [RouteTable] = new() { Type = RouteTable, DisplayName = "Route Table", Service = SupportedServiceTypes.Ec2, Category = "Networking", Color = "#FF9900" },
        [InternetGateway] = new() { Type = InternetGateway, DisplayName = "Internet Gateway", Service = SupportedServiceTypes.Ec2, Category = "Networking", Color = "#8C4FFF" },
        [NatGateway] = new() { Type = NatGateway, DisplayName = "NAT Gateway", Service = SupportedServiceTypes.Ec2, Category = "Networking", Color = "#8C4FFF" },
        [TransitGateway] = new() { Type = TransitGateway, DisplayName = "Transit Gateway", Service = SupportedServiceTypes.Ec2, Category = "Networking", Color = "#8C4FFF" },
        [VpcEndpoint] = new() { Type = VpcEndpoint, DisplayName = "VPC Endpoint", Service = SupportedServiceTypes.Ec2, Category = "Networking", Color = "#8C4FFF" },

        // Compute
        ["ec2.instance"] = new() { Type = "ec2.instance", DisplayName = "EC2 Instance", Service = SupportedServiceTypes.Ec2, Category = "Compute", Color = "#FF9900" },
        [EcsCluster] = new() { Type = EcsCluster, DisplayName = "ECS Cluster", Service = SupportedServiceTypes.Ecs, Category = "Compute", CanContain = true, Color = "#FF9900" },
        [EcsService] = new() { Type = EcsService, DisplayName = "ECS Service", Service = SupportedServiceTypes.Ecs, Category = "Compute", Color = "#FF9900" },
        [EcsTaskDefinition] = new() { Type = EcsTaskDefinition, DisplayName = "Task Definition", Service = SupportedServiceTypes.Ecs, Category = "Compute", Color = "#FF9900" },
        ["lambda.function"] = new() { Type = "lambda.function", DisplayName = "Lambda Function", Service = SupportedServiceTypes.Lambda, Category = "Compute", Color = "#FF9900" },

        // Load Balancing
        [LoadBalancer] = new() { Type = LoadBalancer, DisplayName = "Load Balancer", Service = SupportedServiceTypes.ElbV2, Category = "Networking", Color = "#8C4FFF" },
        ["elbv2.targetgroup"] = new() { Type = "elbv2.targetgroup", DisplayName = "Target Group", Service = SupportedServiceTypes.ElbV2, Category = "Networking", Color = "#8C4FFF" },
        ["elbv2.listener"] = new() { Type = "elbv2.listener", DisplayName = "Listener", Service = SupportedServiceTypes.ElbV2, Category = "Networking", Color = "#8C4FFF" },

        // IAM
        ["iam.role"] = new() { Type = "iam.role", DisplayName = "IAM Role", Service = SupportedServiceTypes.Iam, Category = "Security", Color = "#DD4B39" },
        ["iam.user"] = new() { Type = "iam.user", DisplayName = "IAM User", Service = SupportedServiceTypes.Iam, Category = "Security", Color = "#DD4B39" },
        ["iam.policy"] = new() { Type = "iam.policy", DisplayName = "IAM Policy", Service = SupportedServiceTypes.Iam, Category = "Security", Color = "#DD4B39" },
        ["iam.instanceprofile"] = new() { Type = "iam.instanceprofile", DisplayName = "Instance Profile", Service = SupportedServiceTypes.Iam, Category = "Security", Color = "#DD4B39" },

        // Storage
        ["s3.bucket"] = new() { Type = "s3.bucket", DisplayName = "S3 Bucket", Service = SupportedServiceTypes.S3, Category = "Storage", Color = "#3F8624" },
        ["rds.dbinstance"] = new() { Type = "rds.dbinstance", DisplayName = "RDS Instance", Service = SupportedServiceTypes.Rds, Category = "Database", Color = "#3B48CC" },
        ["rds.dbcluster"] = new() { Type = "rds.dbcluster", DisplayName = "RDS Cluster", Service = SupportedServiceTypes.Rds, Category = "Database", CanContain = true, Color = "#3B48CC" },
        ["dynamodb.table"] = new() { Type = "dynamodb.table", DisplayName = "DynamoDB Table", Service = SupportedServiceTypes.DynamoDb, Category = "Database", Color = "#3B48CC" },

        // Secrets/Config
        ["secretsmanager.secret"] = new() { Type = "secretsmanager.secret", DisplayName = "Secret", Service = SupportedServiceTypes.SecretsManager, Category = "Security", Color = "#DD4B39" },
        ["ssm.parameter"] = new() { Type = "ssm.parameter", DisplayName = "SSM Parameter", Service = SupportedServiceTypes.Ssm, Category = "Management", Color = "#DD4B39" },

        // Messaging
        ["sqs.queue"] = new() { Type = "sqs.queue", DisplayName = "SQS Queue", Service = SupportedServiceTypes.Sqs, Category = "Integration", Color = "#FF4F8B" },
        ["sns.topic"] = new() { Type = "sns.topic", DisplayName = "SNS Topic", Service = SupportedServiceTypes.Sns, Category = "Integration", Color = "#FF4F8B" },

        // CloudWatch
        ["logs.loggroup"] = new() { Type = "logs.loggroup", DisplayName = "Log Group", Service = SupportedServiceTypes.CloudWatchLogs, Category = "Management", Color = "#FF4F8B" },

        // API Gateway
        ["apigateway.restapi"] = new() { Type = "apigateway.restapi", DisplayName = "REST API", Service = SupportedServiceTypes.ApiGateway, Category = "Networking", Color = "#FF4F8B" },
        ["apigatewayv2.api"] = new() { Type = "apigatewayv2.api", DisplayName = "HTTP API", Service = SupportedServiceTypes.ApiGatewayV2, Category = "Networking", Color = "#FF4F8B" },
    };

    /// <summary>
    /// Gets the categories of supported resource types.
    /// </summary>
    public static IEnumerable<string> Categories => All.Values.Select(t => t.Category).Distinct().OrderBy(c => c);

    /// <summary>
    /// Gets resource types by category.
    /// </summary>
    public static IEnumerable<ResourceTypeInfo> GetByCategory(string category) =>
        All.Values.Where(t => t.Category == category).OrderBy(t => t.DisplayName);
}
