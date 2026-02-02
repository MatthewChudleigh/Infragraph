namespace Infragraph.Core.Modeling.ResourceTypes;

using System.Text.Json;
using Common.Models.Domain;
using Common.Models.Former2;

internal sealed class VpcHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);

        return new VpcResource
        {
            Id = resource.Id,
            Arn = GetArn(data, resource.Id, resource.Region),
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name"),
            Tags = tags,
            RawData = data,
            VpcId = GetString(data, "VpcId"),
            CidrBlock = GetString(data, "CidrBlock"),
            IsDefault = GetBool(data, "IsDefault"),
            EnableDnsSupport = GetBool(data, "EnableDnsSupport"),
            EnableDnsHostnames = GetBool(data, "EnableDnsHostnames"),
            State = GetString(data, "State")
        };
    }

    private static string? GetArn(JsonElement data, string id, string? region)
    {
        if (id.StartsWith("arn:")) return id;
        var vpcId = GetString(data, "VpcId") ?? id;
        var ownerId = GetString(data, "OwnerId");
        return ownerId != null ? $"arn:aws:ec2:{region}:{ownerId}:vpc/{vpcId}" : null;
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;

    private static bool GetBool(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.True;
}

internal sealed class SubnetHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);

        return new SubnetResource
        {
            Id = resource.Id,
            Arn = GetString(data, "SubnetArn") ?? resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name"),
            Tags = tags,
            RawData = data,
            SubnetId = GetString(data, "SubnetId"),
            VpcId = GetString(data, "VpcId") ?? "",
            CidrBlock = GetString(data, "CidrBlock"),
            AvailabilityZone = GetString(data, "AvailabilityZone"),
            MapPublicIpOnLaunch = GetBool(data, "MapPublicIpOnLaunch"),
            State = GetString(data, "State")
        };
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;

    private static bool GetBool(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.True;
}

internal sealed class SecurityGroupHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);

        var (ingress, referencedSgs) = ParseRules(data, "IpPermissions");
        var (egress, _) = ParseRules(data, "IpPermissionsEgress");

        return new SecurityGroupResource
        {
            Id = resource.Id,
            Arn = GetArn(data, resource.Id, resource.Region),
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name") ?? GetString(data, "GroupName"),
            Tags = tags,
            RawData = data,
            GroupId = GetString(data, "GroupId"),
            GroupName = GetString(data, "GroupName"),
            VpcId = GetString(data, "VpcId") ?? "",
            Description = GetString(data, "Description"),
            IngressRules = ingress,
            EgressRules = egress,
            ReferencedSecurityGroups = referencedSgs
        };
    }

    private static (List<SecurityGroupRule>, List<string>) ParseRules(JsonElement data, string prop)
    {
        var rules = new List<SecurityGroupRule>();
        var referencedSgs = new List<string>();

        if (!data.TryGetProperty(prop, out var permsElement) || permsElement.ValueKind != JsonValueKind.Array)
            return (rules, referencedSgs);

        foreach (var perm in permsElement.EnumerateArray())
        {
            var cidrBlocks = new List<string>();
            var sgIds = new List<string>();

            if (perm.TryGetProperty("IpRanges", out var ranges))
            {
                foreach (var range in ranges.EnumerateArray())
                {
                    if (range.TryGetProperty("CidrIp", out var cidr))
                        cidrBlocks.Add(cidr.GetString() ?? "");
                }
            }

            if (perm.TryGetProperty("UserIdGroupPairs", out var groups))
            {
                foreach (var grp in groups.EnumerateArray())
                {
                    if (grp.TryGetProperty("GroupId", out var gid))
                    {
                        var sgId = gid.GetString();
                        if (!string.IsNullOrEmpty(sgId))
                        {
                            sgIds.Add(sgId);
                            referencedSgs.Add(sgId);
                        }
                    }
                }
            }

            rules.Add(new SecurityGroupRule
            {
                Protocol = GetString(perm, "IpProtocol"),
                FromPort = GetInt(perm, "FromPort"),
                ToPort = GetInt(perm, "ToPort"),
                CidrBlocks = cidrBlocks,
                SecurityGroupIds = sgIds
            });
        }

        return (rules, referencedSgs.Distinct().ToList());
    }

    private static string? GetArn(JsonElement data, string id, string? region)
    {
        if (id.StartsWith("arn:")) return id;
        var groupId = GetString(data, "GroupId") ?? id;
        var ownerId = GetString(data, "OwnerId");
        return ownerId != null ? $"arn:aws:ec2:{region}:{ownerId}:security-group/{groupId}" : null;
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;

    private static int? GetInt(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number ? val.GetInt32() : null;
}

internal sealed class RouteTableHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);

        return new RouteTableResource
        {
            Id = resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name"),
            Tags = tags,
            RawData = data,
            RouteTableId = GetString(data, "RouteTableId"),
            VpcId = GetString(data, "VpcId") ?? "",
            Routes = ParseRoutes(data),
            AssociatedSubnetIds = ParseAssociations(data)
        };
    }

    private static List<RouteEntry> ParseRoutes(JsonElement data)
    {
        var routes = new List<RouteEntry>();
        if (!data.TryGetProperty("Routes", out var routesEl) || routesEl.ValueKind != JsonValueKind.Array)
            return routes;

        foreach (var r in routesEl.EnumerateArray())
        {
            routes.Add(new RouteEntry
            {
                DestinationCidrBlock = GetString(r, "DestinationCidrBlock"),
                GatewayId = GetString(r, "GatewayId"),
                NatGatewayId = GetString(r, "NatGatewayId"),
                TransitGatewayId = GetString(r, "TransitGatewayId"),
                VpcEndpointId = GetString(r, "VpcEndpointId")
            });
        }
        return routes;
    }

    private static List<string> ParseAssociations(JsonElement data)
    {
        var subnetIds = new List<string>();
        if (!data.TryGetProperty("Associations", out var assocs) || assocs.ValueKind != JsonValueKind.Array)
            return subnetIds;

        foreach (var a in assocs.EnumerateArray())
        {
            var subnetId = GetString(a, "SubnetId");
            if (!string.IsNullOrEmpty(subnetId))
                subnetIds.Add(subnetId);
        }
        return subnetIds;
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;
}

internal sealed class InternetGatewayHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);

        var attachedVpcs = ExtractAttachedVpcs(data);

        return new InternetGatewayResource
        {
            Id = resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name"),
            Tags = tags,
            RawData = data,
            InternetGatewayId = GetString(data, "InternetGatewayId"),
            AttachedVpcIds = attachedVpcs
        };
    }

    private static List<string> ExtractAttachedVpcs(JsonElement data)
    {
        var attachedVpcs = new List<string>();
        if (!data.TryGetProperty("Attachments", out var atts) || atts.ValueKind != JsonValueKind.Array)
            return attachedVpcs;
        
        foreach (var att in atts.EnumerateArray())
        {
            if (att.TryGetProperty("VpcId", out var vpcId))
                attachedVpcs.Add(vpcId.GetString() ?? "");
        }

        return attachedVpcs;
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;
}

internal sealed class NatGatewayHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);

        return new NatGatewayResource
        {
            Id = resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name"),
            Tags = tags,
            RawData = data,
            NatGatewayId = GetString(data, "NatGatewayId"),
            SubnetId = GetString(data, "SubnetId"),
            VpcId = GetString(data, "VpcId"),
            State = GetString(data, "State")
        };
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;
}

internal sealed class TransitGatewayHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);

        return new TransitGatewayResource
        {
            Id = resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name"),
            Tags = tags,
            RawData = data,
            TransitGatewayId = GetString(data, "TransitGatewayId"),
            State = GetString(data, "State")
        };
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;
}
