namespace Infragraph.Core.Modeling.ResourceTypes;

using System.Text.Json;
using Infragraph.Common.Models.Domain;
using Infragraph.Common.Models.Former2;

internal sealed class IamRoleHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);

        var policyArns = new List<string>();
        if (data.TryGetProperty("AttachedPolicies", out var policies) && policies.ValueKind == JsonValueKind.Array)
        {
            foreach (var p in policies.EnumerateArray())
            {
                if (p.TryGetProperty("PolicyArn", out var arn))
                    policyArns.Add(arn.GetString() ?? "");
            }
        }

        return new IamRoleResource
        {
            Id = resource.Id,
            Arn = GetString(data, "Arn") ?? resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name") ?? GetString(data, "RoleName"),
            Tags = tags,
            RawData = data,
            RoleArn = GetString(data, "Arn"),
            RoleName = GetString(data, "RoleName"),
            Path = GetString(data, "Path"),
            Description = GetString(data, "Description"),
            AssumeRolePolicyDocument = GetString(data, "AssumeRolePolicyDocument"),
            AttachedPolicyArns = policyArns,
            MaxSessionDuration = GetIntNullable(data, "MaxSessionDuration")
        };
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;

    private static int? GetIntNullable(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number ? val.GetInt32() : null;
}

internal sealed class IamUserHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);

        var policyArns = new List<string>();
        if (data.TryGetProperty("AttachedPolicies", out var policies) && policies.ValueKind == JsonValueKind.Array)
        {
            foreach (var p in policies.EnumerateArray())
            {
                if (p.TryGetProperty("PolicyArn", out var arn))
                    policyArns.Add(arn.GetString() ?? "");
            }
        }

        var groups = new List<string>();
        if (data.TryGetProperty("Groups", out var grps) && grps.ValueKind == JsonValueKind.Array)
        {
            foreach (var g in grps.EnumerateArray())
            {
                if (g.ValueKind == JsonValueKind.String)
                    groups.Add(g.GetString() ?? "");
                else if (g.TryGetProperty("GroupName", out var gn))
                    groups.Add(gn.GetString() ?? "");
            }
        }

        return new IamUserResource
        {
            Id = resource.Id,
            Arn = GetString(data, "Arn") ?? resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name") ?? GetString(data, "UserName"),
            Tags = tags,
            RawData = data,
            UserArn = GetString(data, "Arn"),
            UserName = GetString(data, "UserName"),
            Path = GetString(data, "Path"),
            AttachedPolicyArns = policyArns,
            Groups = groups
        };
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;
}

internal sealed class IamPolicyHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);

        return new IamPolicyResource
        {
            Id = resource.Id,
            Arn = GetString(data, "Arn") ?? resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name") ?? GetString(data, "PolicyName"),
            Tags = tags,
            RawData = data,
            PolicyArn = GetString(data, "Arn"),
            PolicyName = GetString(data, "PolicyName"),
            Path = GetString(data, "Path"),
            Description = GetString(data, "Description"),
            IsAttachable = GetBool(data, "IsAttachable"),
            AttachmentCount = GetInt(data, "AttachmentCount")
        };
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;

    private static bool GetBool(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.True;

    private static int GetInt(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number ? val.GetInt32() : 0;
}

internal sealed class InstanceProfileHandler : IResourceTypeHandler
{
    public AwsResource CreateResource(Former2Resource resource)
    {
        var data = resource.Data;
        var tags = ResourceModelFactory.ExtractTags(data);

        var roleArns = new List<string>();
        if (data.TryGetProperty("Roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in roles.EnumerateArray())
            {
                if (r.TryGetProperty("Arn", out var arn))
                    roleArns.Add(arn.GetString() ?? "");
            }
        }

        return new InstanceProfileResource
        {
            Id = resource.Id,
            Arn = GetString(data, "Arn") ?? resource.Id,
            Type = resource.Type,
            Region = resource.Region,
            Name = tags.GetValueOrDefault("Name") ?? GetString(data, "InstanceProfileName"),
            Tags = tags,
            RawData = data,
            InstanceProfileArn = GetString(data, "Arn"),
            InstanceProfileName = GetString(data, "InstanceProfileName"),
            Path = GetString(data, "Path"),
            RoleArns = roleArns
        };
    }

    private static string? GetString(JsonElement data, string prop) =>
        data.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;
}
