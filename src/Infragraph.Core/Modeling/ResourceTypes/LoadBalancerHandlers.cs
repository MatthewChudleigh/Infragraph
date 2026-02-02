namespace Infragraph.Core.Modeling.ResourceTypes;

using System.Text.Json;
using Common.Models.Domain;
using Common.Models.Former2;

internal sealed class LoadBalancerHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);

        var subnets = new List<string>();
        var azs = new List<string>();
        if (data.TryGetProperty("AvailabilityZones", out var zones) && zones.ValueKind == JsonValueKind.Array)
        {
            foreach (var z in zones.EnumerateArray())
            {
                if (z.TryGetProperty("SubnetId", out var sub))
                    subnets.Add(sub.GetString() ?? "");
                if (z.TryGetProperty("ZoneName", out var zn))
                    azs.Add(zn.GetString() ?? "");
            }
        }

        var securityGroups = ExtractSecurityGroups(data);

        return new LoadBalancerResource
        {
            Id = resource.Id,
            Arn = GetString(data, "LoadBalancerArn") ?? resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name") ?? GetString(data, "LoadBalancerName"),
            Tags = tags,
            RawData = data,
            LoadBalancerArn = GetString(data, "LoadBalancerArn"),
            LoadBalancerName = GetString(data, "LoadBalancerName"),
            DnsName = GetString(data, "DNSName"),
            VpcId = GetString(data, "VpcId") ?? "",
            Scheme = GetString(data, "Scheme"),
            LoadBalancerType = GetString(data, "Type"),
            State = GetNestedString(data, "State", "Code"),
            SubnetIds = subnets,
            SecurityGroupIds = securityGroups,
            AvailabilityZones = azs
        };
    }

    private static List<string> ExtractSecurityGroups(JsonElement data)
    {
        var securityGroups = new List<string>();
        if (!data.TryGetProperty("SecurityGroups", out var sgs) || sgs.ValueKind != JsonValueKind.Array)
            return securityGroups;
        
        securityGroups.AddRange(sgs.EnumerateArray().Select(sg => sg.GetString() ?? ""));

        return securityGroups;
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;

    private static string? GetNestedString(JsonElement data, string prop, string nested) =>
        data.TryGetProperty(prop, out var obj) && obj.TryGetProperty(nested, out var val) && val.ValueKind == JsonValueKind.String
            ? val.GetString() : null;
}

internal sealed class TargetGroupHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);

        var lbArns = ExtractLbArns(data);

        return new TargetGroupResource
        {
            Id = resource.Id,
            Arn = GetString(data, "TargetGroupArn") ?? resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name") ?? GetString(data, "TargetGroupName"),
            Tags = tags,
            RawData = data,
            TargetGroupArn = GetString(data, "TargetGroupArn"),
            TargetGroupName = GetString(data, "TargetGroupName"),
            VpcId = GetString(data, "VpcId"),
            Protocol = GetString(data, "Protocol"),
            Port = GetIntNullable(data, "Port"),
            TargetType = GetString(data, "TargetType"),
            HealthCheckPath = GetString(data, "HealthCheckPath"),
            LoadBalancerArns = lbArns
        };
    }

    private static List<string> ExtractLbArns(JsonElement data)
    {
        var lbArns = new List<string>();
        if (!data.TryGetProperty("LoadBalancerArns", out var lbs) || lbs.ValueKind != JsonValueKind.Array)
            return lbArns;
        
        lbArns.AddRange(lbs.EnumerateArray().Select(lb => lb.GetString() ?? ""));

        return lbArns;
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;

    private static int? GetIntNullable(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number ? val.GetInt32() : null;
}

internal sealed class ListenerHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;

        var actions = ExtractListenerActions(data);

        return new ListenerResource
        {
            Id = resource.Id,
            Arn = GetString(data, "ListenerArn") ?? resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            RawData = data,
            ListenerArn = GetString(data, "ListenerArn"),
            LoadBalancerArn = GetString(data, "LoadBalancerArn") ?? "",
            Port = GetIntNullable(data, "Port"),
            Protocol = GetString(data, "Protocol"),
            DefaultActions = actions
        };
    }

    private static List<ListenerAction> ExtractListenerActions(JsonElement data)
    {
        var actions = new List<ListenerAction>();
        if (!data.TryGetProperty("DefaultActions", out var acts) || acts.ValueKind != JsonValueKind.Array)
            return actions;
        
        foreach (var a in acts.EnumerateArray())
        {
            actions.Add(new ListenerAction
            {
                Type = GetString(a, "Type"),
                TargetGroupArn = GetString(a, "TargetGroupArn"),
                Order = GetIntNullable(a, "Order")
            });
        }

        return actions;
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;

    private static int? GetIntNullable(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number ? val.GetInt32() : null;
}
