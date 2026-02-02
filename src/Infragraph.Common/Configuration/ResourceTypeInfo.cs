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

/// <summary>
/// Registry of supported resource types.
/// </summary>
public static class SupportedResourceTypes
{
    public const string LoadBalancer = "elbv2.loadbalancer";
    
    /// <summary>
    /// All supported resource types.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, ResourceTypeInfo> All = new Dictionary<string, ResourceTypeInfo>
    {
        // VPC/Networking
        ["ec2.vpc"] = new() { Type = "ec2.vpc", DisplayName = "VPC", Service = "ec2", Category = "Networking", CanContain = true, Color = "#FF9900" },
        ["ec2.subnet"] = new() { Type = "ec2.subnet", DisplayName = "Subnet", Service = "ec2", Category = "Networking", CanContain = true, Color = "#FF9900" },
        ["ec2.securitygroup"] = new() { Type = "ec2.securitygroup", DisplayName = "Security Group", Service = "ec2", Category = "Networking", Color = "#DD4B39" },
        ["ec2.routetable"] = new() { Type = "ec2.routetable", DisplayName = "Route Table", Service = "ec2", Category = "Networking", Color = "#FF9900" },
        ["ec2.internetgateway"] = new() { Type = "ec2.internetgateway", DisplayName = "Internet Gateway", Service = "ec2", Category = "Networking", Color = "#8C4FFF" },
        ["ec2.natgateway"] = new() { Type = "ec2.natgateway", DisplayName = "NAT Gateway", Service = "ec2", Category = "Networking", Color = "#8C4FFF" },
        ["ec2.transitgateway"] = new() { Type = "ec2.transitgateway", DisplayName = "Transit Gateway", Service = "ec2", Category = "Networking", Color = "#8C4FFF" },
        ["ec2.vpcendpoint"] = new() { Type = "ec2.vpcendpoint", DisplayName = "VPC Endpoint", Service = "ec2", Category = "Networking", Color = "#8C4FFF" },

        // Compute
        ["ec2.instance"] = new() { Type = "ec2.instance", DisplayName = "EC2 Instance", Service = "ec2", Category = "Compute", Color = "#FF9900" },
        ["ecs.cluster"] = new() { Type = "ecs.cluster", DisplayName = "ECS Cluster", Service = "ecs", Category = "Compute", CanContain = true, Color = "#FF9900" },
        ["ecs.service"] = new() { Type = "ecs.service", DisplayName = "ECS Service", Service = "ecs", Category = "Compute", Color = "#FF9900" },
        ["ecs.taskdefinition"] = new() { Type = "ecs.taskdefinition", DisplayName = "Task Definition", Service = "ecs", Category = "Compute", Color = "#FF9900" },
        ["lambda.function"] = new() { Type = "lambda.function", DisplayName = "Lambda Function", Service = "lambda", Category = "Compute", Color = "#FF9900" },

        // Load Balancing
        [LoadBalancer] = new() { Type = LoadBalancer, DisplayName = "Load Balancer", Service = "elbv2", Category = "Networking", Color = "#8C4FFF" },
        ["elbv2.targetgroup"] = new() { Type = "elbv2.targetgroup", DisplayName = "Target Group", Service = "elbv2", Category = "Networking", Color = "#8C4FFF" },
        ["elbv2.listener"] = new() { Type = "elbv2.listener", DisplayName = "Listener", Service = "elbv2", Category = "Networking", Color = "#8C4FFF" },

        // IAM
        ["iam.role"] = new() { Type = "iam.role", DisplayName = "IAM Role", Service = "iam", Category = "Security", Color = "#DD4B39" },
        ["iam.user"] = new() { Type = "iam.user", DisplayName = "IAM User", Service = "iam", Category = "Security", Color = "#DD4B39" },
        ["iam.policy"] = new() { Type = "iam.policy", DisplayName = "IAM Policy", Service = "iam", Category = "Security", Color = "#DD4B39" },
        ["iam.instanceprofile"] = new() { Type = "iam.instanceprofile", DisplayName = "Instance Profile", Service = "iam", Category = "Security", Color = "#DD4B39" },

        // Storage
        ["s3.bucket"] = new() { Type = "s3.bucket", DisplayName = "S3 Bucket", Service = "s3", Category = "Storage", Color = "#3F8624" },
        ["rds.dbinstance"] = new() { Type = "rds.dbinstance", DisplayName = "RDS Instance", Service = "rds", Category = "Database", Color = "#3B48CC" },
        ["rds.dbcluster"] = new() { Type = "rds.dbcluster", DisplayName = "RDS Cluster", Service = "rds", Category = "Database", CanContain = true, Color = "#3B48CC" },
        ["dynamodb.table"] = new() { Type = "dynamodb.table", DisplayName = "DynamoDB Table", Service = "dynamodb", Category = "Database", Color = "#3B48CC" },

        // Secrets/Config
        ["secretsmanager.secret"] = new() { Type = "secretsmanager.secret", DisplayName = "Secret", Service = "secretsmanager", Category = "Security", Color = "#DD4B39" },
        ["ssm.parameter"] = new() { Type = "ssm.parameter", DisplayName = "SSM Parameter", Service = "ssm", Category = "Management", Color = "#DD4B39" },

        // Messaging
        ["sqs.queue"] = new() { Type = "sqs.queue", DisplayName = "SQS Queue", Service = "sqs", Category = "Integration", Color = "#FF4F8B" },
        ["sns.topic"] = new() { Type = "sns.topic", DisplayName = "SNS Topic", Service = "sns", Category = "Integration", Color = "#FF4F8B" },

        // CloudWatch
        ["logs.loggroup"] = new() { Type = "logs.loggroup", DisplayName = "Log Group", Service = "logs", Category = "Management", Color = "#FF4F8B" },

        // API Gateway
        ["apigateway.restapi"] = new() { Type = "apigateway.restapi", DisplayName = "REST API", Service = "apigateway", Category = "Networking", Color = "#FF4F8B" },
        ["apigatewayv2.api"] = new() { Type = "apigatewayv2.api", DisplayName = "HTTP API", Service = "apigatewayv2", Category = "Networking", Color = "#FF4F8B" },
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
