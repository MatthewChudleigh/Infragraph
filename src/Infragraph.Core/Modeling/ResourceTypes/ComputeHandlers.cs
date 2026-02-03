namespace Infragraph.Core.Modeling.ResourceTypes;

using System.Text.Json;
using Common.Models.Domain;
using Common.Models.Former2;

internal sealed class Ec2InstanceHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);
        var sgIds = ExtractSecurityGroupIds(data);

        return new Ec2InstanceResource
        {
            Id = resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name"),
            Tags = tags,
            RawData = data,
            InstanceId = data.GetString("InstanceId"),
            InstanceType = data.GetString("InstanceType"),
            SubnetId = data.GetString("SubnetId"),
            VpcId = data.GetString("VpcId"),
            State = data.GetNestedString("State", "Name"),
            PrivateIpAddress = data.GetString("PrivateIpAddress"),
            PublicIpAddress = data.GetString("PublicIpAddress"),
            SecurityGroupIds = sgIds,
            IamInstanceProfileArn = data.GetNestedString("IamInstanceProfile", "Arn")
        };
    }

    private static List<string> ExtractSecurityGroupIds(JsonElement data)
    {
        var sgIds = new List<string>();
        if (!data.TryGetProperty("SecurityGroups", out var sgs) || sgs.ValueKind != JsonValueKind.Array) 
            return sgIds;
        
        foreach (var sg in sgs.EnumerateArray())
        {
            if (sg.TryGetProperty("GroupId", out var gid))
                sgIds.Add(gid.GetString() ?? "");
        }

        return sgIds;
    }
}

internal sealed class EbsVolumeHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);
        var attachments = ExtractVolumeAttachments(data);

        return new EbsVolumeResource
        {
            Id = resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name"),
            Tags = tags,
            RawData = data,
            VolumeId = data.GetString("VolumeId"),
            AvailabilityZone = data.GetString("AvailabilityZone"),
            VolumeType = data.GetString("VolumeType"),
            Size = data.GetIntNullable("Size"),
            State = data.GetString("State"),
            Encrypted = data.GetBool("Encrypted"),
            Attachments = attachments
        };
    }

    private static List<VolumeAttachment> ExtractVolumeAttachments(JsonElement data)
    {
        var attachments = new List<VolumeAttachment>();
        if (!data.TryGetProperty("Attachments", out var atts) || atts.ValueKind != JsonValueKind.Array)
            return attachments;
        
        foreach (var att in atts.EnumerateArray())
        {
            attachments.Add(new VolumeAttachment
            {
                InstanceId = att.GetString("InstanceId"),
                Device = att.GetString("Device"),
                State = att.GetString("State")
            });
        }

        return attachments;
    }
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
            Arn = data.GetString("clusterArn") ?? resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name") ?? data.GetString("clusterName"),
            Tags = tags,
            RawData = data,
            ClusterArn = data.GetString("clusterArn"),
            ClusterName = data.GetString("clusterName"),
            Status = data.GetString("status"),
            RunningTasksCount = data.GetInt("runningTasksCount"),
            ActiveServicesCount = data.GetInt("activeServicesCount")
        };
    }
}

internal sealed class EcsServiceHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);
        var loadBalancers = ExtractLoadBalancers(data);
        var (subnets, securityGroups) = ParseNetworkConfig(data);

        return new EcsServiceResource
        {
            Id = resource.Id,
            Arn = data.GetString("serviceArn") ?? resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name") ?? data.GetString("serviceName"),
            Tags = tags,
            RawData = data,
            ServiceArn = data.GetString("serviceArn"),
            EcsServiceName = data.GetString("serviceName"),
            ClusterArn = data.GetString("clusterArn") ?? "",
            TaskDefinitionArn = data.GetString("taskDefinition"),
            Status = data.GetString("status"),
            DesiredCount = data.GetInt("desiredCount"),
            RunningCount = data.GetInt("runningCount"),
            LaunchType = data.GetString("launchType"),
            SubnetIds = subnets,
            SecurityGroupIds = securityGroups,
            LoadBalancers = loadBalancers,
            RoleArn = data.GetString("roleArn")
        };
    }

    private static List<LoadBalancerAttachment> ExtractLoadBalancers(JsonElement data)
    {
        var loadBalancers = new List<LoadBalancerAttachment>();
        if (!data.TryGetProperty("loadBalancers", out var lbs) || lbs.ValueKind != JsonValueKind.Array)
            return loadBalancers;
        
        foreach (var lb in lbs.EnumerateArray())
        {
            loadBalancers.Add(new LoadBalancerAttachment
            {
                TargetGroupArn = lb.GetString( "targetGroupArn"),
                ContainerName = lb.GetString("containerName"),
                ContainerPort = lb.GetIntNullable("containerPort")
            });
        }

        return loadBalancers;
    }

    private static (List<string>, List<string>) ParseNetworkConfig(JsonElement data)
    {
        var subnets = new List<string>();
        var securityGroups = new List<string>();

        if (!data.TryGetProperty("networkConfiguration", out var nc) ||
            !nc.TryGetProperty("awsvpcConfiguration", out var vpc)) 
            return (subnets, securityGroups);
        
        if (vpc.TryGetProperty("subnets", out var subs) && subs.ValueKind == JsonValueKind.Array)
        {
            subnets.AddRange(subs.EnumerateArray().Select(s => s.GetString() ?? ""));
        }
        
        if (vpc.TryGetProperty("securityGroups", out var sgs) && sgs.ValueKind == JsonValueKind.Array)
        {
            securityGroups.AddRange(sgs.EnumerateArray().Select(sg => sg.GetString() ?? ""));
        }

        return (subnets, securityGroups);
    }
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
                var portMappings = ExtractPortMappings(c);

                containers.Add(new ContainerDefinition
                {
                    Name = c.GetString("name"),
                    Image = c.GetString("image"),
                    Cpu = c.GetIntNullable("cpu"),
                    Memory = c.GetIntNullable( "memory"),
                    PortMappings = portMappings
                });
            }
        }

        return new EcsTaskDefinitionResource
        {
            Id = resource.Id,
            Arn = data.GetString("taskDefinitionArn") ?? resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name") ?? data.GetString("family"),
            Tags = tags,
            RawData = data,
            TaskDefinitionArn = data.GetString("taskDefinitionArn"),
            Family = data.GetString("family"),
            Revision = data.GetInt("revision"),
            TaskRoleArn = data.GetString("taskRoleArn"),
            ExecutionRoleArn = data.GetString("executionRoleArn"),
            NetworkMode = data.GetString("networkMode"),
            Cpu = data.GetIntNullable( "cpu") ?? ParseCpuMemory(data.GetString("cpu")),
            Memory = data.GetIntNullable( "memory") ?? ParseCpuMemory(data.GetString("memory")),
            Containers = containers
        };
    }

    private static List<PortMapping> ExtractPortMappings(JsonElement containerDefinition)
    {
        var portMappings = new List<PortMapping>();
        if (!containerDefinition.TryGetProperty("portMappings", out var pms) || pms.ValueKind != JsonValueKind.Array)
            return portMappings;
        
        foreach (var pm in pms.EnumerateArray())
        {
            portMappings.Add(new PortMapping
            {
                ContainerPort = pm.GetIntNullable("containerPort"),
                HostPort = pm.GetIntNullable("hostPort"),
                Protocol = pm.GetString("protocol")
            });
        }

        return portMappings;
    }

    private static int? ParseCpuMemory(string? val)
    {
        if (string.IsNullOrEmpty(val)) return null;
        return int.TryParse(val, out var result) ? result : null;
    }
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
            Arn = data.GetString("FunctionArn") ?? resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name") ?? data.GetString("FunctionName"),
            Tags = tags,
            RawData = data,
            FunctionArn = data.GetString("FunctionArn"),
            FunctionName = data.GetString("FunctionName"),
            Runtime = data.GetString("Runtime"),
            Handler = data.GetString("Handler"),
            RoleArn = data.GetString("Role"),
            Timeout = data.GetIntNullable( "Timeout"),
            MemorySize = data.GetIntNullable( "MemorySize"),
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

        if (!data.TryGetProperty("VpcConfig", out var vpc)) return (subnets, securityGroups, vpcId);
        
        vpcId = vpc.GetString("VpcId");
        if (vpc.TryGetProperty("SubnetIds", out var subs) && subs.ValueKind == JsonValueKind.Array)
        {
            subnets.AddRange(subs.EnumerateArray().Select(s => s.GetString() ?? ""));
        }
        
        if (vpc.TryGetProperty("SecurityGroupIds", out var sgs) && sgs.ValueKind == JsonValueKind.Array)
        {
            securityGroups.AddRange(sgs.EnumerateArray().Select(sg => sg.GetString() ?? ""));
        }

        return (subnets, securityGroups, vpcId);
    }
}
