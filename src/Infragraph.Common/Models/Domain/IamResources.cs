namespace Infragraph.Common.Models.Domain;

/// <summary>
/// IAM Role resource model.
/// </summary>
public sealed class IamRoleResource : AwsResource
{
    public string? RoleArn { get; init; }
    public string? RoleName { get; init; }
    public string? Path { get; init; }
    public string? Description { get; init; }
    public string? AssumeRolePolicyDocument { get; init; }
    public List<string> AttachedPolicyArns { get; init; } = [];
    public int? MaxSessionDuration { get; init; }

    public override string DisplayName => Name ?? RoleName ?? base.DisplayName;
}

/// <summary>
/// IAM User resource model.
/// </summary>
public sealed class IamUserResource : AwsResource
{
    public string? UserArn { get; init; }
    public string? UserName { get; init; }
    public string? Path { get; init; }
    public List<string> AttachedPolicyArns { get; init; } = [];
    public List<string> Groups { get; init; } = [];

    public override string DisplayName => Name ?? UserName ?? base.DisplayName;
}

/// <summary>
/// IAM Policy resource model.
/// </summary>
public sealed class IamPolicyResource : AwsResource
{
    public string? PolicyArn { get; init; }
    public string? PolicyName { get; init; }
    public string? Path { get; init; }
    public string? Description { get; init; }
    public bool IsAttachable { get; init; }
    public int AttachmentCount { get; init; }

    public override string DisplayName => Name ?? PolicyName ?? base.DisplayName;
}

/// <summary>
/// IAM Instance Profile resource model.
/// </summary>
public sealed class InstanceProfileResource : AwsResource
{
    public string? InstanceProfileArn { get; init; }
    public string? InstanceProfileName { get; init; }
    public string? Path { get; init; }
    public List<string> RoleArns { get; init; } = [];

    public override string DisplayName => Name ?? InstanceProfileName ?? base.DisplayName;
}
