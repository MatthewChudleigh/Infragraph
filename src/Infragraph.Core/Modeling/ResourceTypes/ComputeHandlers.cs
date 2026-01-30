namespace Infragraph.Core.Modeling.ResourceTypes;

using System.Text.Json;
using Infragraph.Common.Models.Domain;
using Infragraph.Common.Models.Former2;

internal sealed class Ec2InstanceHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);

        var sgIds = new List<string>();
        if (data.TryGetProperty("SecurityGroups", out var sgs) && sgs.ValueKind == JsonValueKind.Array)
        {
            foreach (var sg in sgs.EnumerateArray())
            {
                if (sg.TryGetProperty("GroupId", out var gid))
                    sgIds.Add(gid.GetString() ?? "");
            }
        }

        return new Ec2InstanceResource
        {
            Id = resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name"),
            Tags = tags,
            RawData = data,
            InstanceId = GetString(data, "InstanceId"),
            InstanceType = GetString(data, "InstanceType"),
            SubnetId = GetString(data, "SubnetId"),
            VpcId = GetString(data, "VpcId"),
            State = GetNestedString(data, "State", "Name"),
            PrivateIpAddress = GetString(data, "PrivateIpAddress"),
            PublicIpAddress = GetString(data, "PublicIpAddress"),
            SecurityGroupIds = sgIds,
            IamInstanceProfileArn = GetNestedString(data, "IamInstanceProfile", "Arn")
        };
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;

    private static string? GetNestedString(JsonElement data, string prop, string nested) =>
        data.TryGetProperty(prop, out var obj) && obj.TryGetProperty(nested, out var val) && val.ValueKind == JsonValueKind.String
            ? val.GetString() : null;
}

internal sealed class EbsVolumeHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);

        var attachments = new List<VolumeAttachment>();
        if (data.TryGetProperty("Attachments", out var atts) && atts.ValueKind == JsonValueKind.Array)
        {
            foreach (var att in atts.EnumerateArray())
            {
                attachments.Add(new VolumeAttachment
                {
                    InstanceId = GetString(att, "InstanceId"),
                    Device = GetString(att, "Device"),
                    State = GetString(att, "State")
                });
            }
        }

        return new EbsVolumeResource
        {
            Id = resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name"),
            Tags = tags,
            RawData = data,
            VolumeId = GetString(data, "VolumeId"),
            AvailabilityZone = GetString(data, "AvailabilityZone"),
            VolumeType = GetString(data, "VolumeType"),
            Size = GetIntNullable(data, "Size"),
            State = GetString(data, "State"),
            Encrypted = GetBool(data, "Encrypted"),
            Attachments = attachments
        };
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;

    private static int? GetIntNullable(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number ? val.GetInt32() : null;

    private static bool GetBool(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.True;
}

internal sealed class EcsClusterHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);

        return new EcsClusterResource
        {
            Id = resource.Id,
            Arn = GetString(data, "clusterArn") ?? resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name") ?? GetString(data, "clusterName"),
            Tags = tags,
            RawData = data,
            ClusterArn = GetString(data, "clusterArn"),
            ClusterName = GetString(data, "clusterName"),
            Status = GetString(data, "status"),
            RunningTasksCount = GetInt(data, "runningTasksCount"),
            ActiveServicesCount = GetInt(data, "activeServicesCount")
        };
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;

    private static int GetInt(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number ? val.GetInt32() : 0;
}

internal sealed class EcsServiceHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);

        var loadBalancers = new List<LoadBalancerAttachment>();
        if (data.TryGetProperty("loadBalancers", out var lbs) && lbs.ValueKind == JsonValueKind.Array)
        {
            foreach (var lb in lbs.EnumerateArray())
            {
                loadBalancers.Add(new LoadBalancerAttachment
                {
                    TargetGroupArn = GetString(lb, "targetGroupArn"),
                    ContainerName = GetString(lb, "containerName"),
                    ContainerPort = GetIntNullable(lb, "containerPort")
                });
            }
        }

        var (subnets, securityGroups) = ParseNetworkConfig(data);

        return new EcsServiceResource
        {
            Id = resource.Id,
            Arn = GetString(data, "serviceArn") ?? resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name") ?? GetString(data, "serviceName"),
            Tags = tags,
            RawData = data,
            ServiceArn = GetString(data, "serviceArn"),
            EcsServiceName = GetString(data, "serviceName"),
            ClusterArn = GetString(data, "clusterArn") ?? "",
            TaskDefinitionArn = GetString(data, "taskDefinition"),
            Status = GetString(data, "status"),
            DesiredCount = GetInt(data, "desiredCount"),
            RunningCount = GetInt(data, "runningCount"),
            LaunchType = GetString(data, "launchType"),
            SubnetIds = subnets,
            SecurityGroupIds = securityGroups,
            LoadBalancers = loadBalancers,
            RoleArn = GetString(data, "roleArn")
        };
    }

    private static (List<string>, List<string>) ParseNetworkConfig(JsonElement data)
    {
        var subnets = new List<string>();
        var securityGroups = new List<string>();

        if (data.TryGetProperty("networkConfiguration", out var nc) &&
            nc.TryGetProperty("awsvpcConfiguration", out var vpc))
        {
            if (vpc.TryGetProperty("subnets", out var subs) && subs.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in subs.EnumerateArray())
                    subnets.Add(s.GetString() ?? "");
            }
            if (vpc.TryGetProperty("securityGroups", out var sgs) && sgs.ValueKind == JsonValueKind.Array)
            {
                foreach (var sg in sgs.EnumerateArray())
                    securityGroups.Add(sg.GetString() ?? "");
            }
        }

        return (subnets, securityGroups);
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;

    private static int GetInt(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number ? val.GetInt32() : 0;

    private static int? GetIntNullable(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number ? val.GetInt32() : null;
}

internal sealed class EcsTaskDefinitionHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);

        var containers = new List<ContainerDefinition>();
        if (data.TryGetProperty("containerDefinitions", out var defs) && defs.ValueKind == JsonValueKind.Array)
        {
            foreach (var c in defs.EnumerateArray())
            {
                var portMappings = new List<PortMapping>();
                if (c.TryGetProperty("portMappings", out var pms) && pms.ValueKind == JsonValueKind.Array)
                {
                    foreach (var pm in pms.EnumerateArray())
                    {
                        portMappings.Add(new PortMapping
                        {
                            ContainerPort = GetIntNullable(pm, "containerPort"),
                            HostPort = GetIntNullable(pm, "hostPort"),
                            Protocol = GetString(pm, "protocol")
                        });
                    }
                }

                containers.Add(new ContainerDefinition
                {
                    Name = GetString(c, "name"),
                    Image = GetString(c, "image"),
                    Cpu = GetIntNullable(c, "cpu"),
                    Memory = GetIntNullable(c, "memory"),
                    PortMappings = portMappings
                });
            }
        }

        return new EcsTaskDefinitionResource
        {
            Id = resource.Id,
            Arn = GetString(data, "taskDefinitionArn") ?? resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name") ?? GetString(data, "family"),
            Tags = tags,
            RawData = data,
            TaskDefinitionArn = GetString(data, "taskDefinitionArn"),
            Family = GetString(data, "family"),
            Revision = GetInt(data, "revision"),
            TaskRoleArn = GetString(data, "taskRoleArn"),
            ExecutionRoleArn = GetString(data, "executionRoleArn"),
            NetworkMode = GetString(data, "networkMode"),
            Cpu = GetIntNullable(data, "cpu") ?? ParseCpuMemory(GetString(data, "cpu")),
            Memory = GetIntNullable(data, "memory") ?? ParseCpuMemory(GetString(data, "memory")),
            Containers = containers
        };
    }

    private static int? ParseCpuMemory(string? val)
    {
        if (string.IsNullOrEmpty(val)) return null;
        return int.TryParse(val, out var result) ? result : null;
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;

    private static int GetInt(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number ? val.GetInt32() : 0;

    private static int? GetIntNullable(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number ? val.GetInt32() : null;
}

internal sealed class LambdaFunctionHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);

        var (subnets, securityGroups, vpcId) = ParseVpcConfig(data);

        return new LambdaFunctionResource
        {
            Id = resource.Id,
            Arn = GetString(data, "FunctionArn") ?? resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name") ?? GetString(data, "FunctionName"),
            Tags = tags,
            RawData = data,
            FunctionArn = GetString(data, "FunctionArn"),
            FunctionName = GetString(data, "FunctionName"),
            Runtime = GetString(data, "Runtime"),
            Handler = GetString(data, "Handler"),
            RoleArn = GetString(data, "Role"),
            Timeout = GetIntNullable(data, "Timeout"),
            MemorySize = GetIntNullable(data, "MemorySize"),
            VpcId = vpcId,
            SubnetIds = subnets,
            SecurityGroupIds = securityGroups
        };
    }

    private static (List<string>, List<string>, string?) ParseVpcConfig(JsonElement data)
    {
        var subnets = new List<string>();
        var securityGroups = new List<string>();
        string? vpcId = null;

        if (data.TryGetProperty("VpcConfig", out var vpc))
        {
            vpcId = GetString(vpc, "VpcId");
            if (vpc.TryGetProperty("SubnetIds", out var subs) && subs.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in subs.EnumerateArray())
                    subnets.Add(s.GetString() ?? "");
            }
            if (vpc.TryGetProperty("SecurityGroupIds", out var sgs) && sgs.ValueKind == JsonValueKind.Array)
            {
                foreach (var sg in sgs.EnumerateArray())
                    securityGroups.Add(sg.GetString() ?? "");
            }
        }

        return (subnets, securityGroups, vpcId);
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;

    private static int? GetIntNullable(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number ? val.GetInt32() : null;
}
