using Infragraph.Common.Abstractions;

namespace Infragraph.Core.Relationships;

public static class AllRelationships
{
    public static ICollection<IRelationshipExtractor> All()
    {
        return
        [
            new ComputeRelationship(),
            new EcsRelationship(),
            new ElbRelationship(),
            new IamRelationship(),
            new NetworkRelationship(),
            new SecurityRelationship()
        ];
    }
    
    public static IEnumerable<string> NetworkingResourceTypes => [
        Common.Configuration.SupportedResourceTypes.Vpc, 
        Common.Configuration.SupportedResourceTypes.Subnet, 
        Common.Configuration.SupportedResourceTypes.Route, 
        Common.Configuration.SupportedResourceTypes.RouteTable,
        Common.Configuration.SupportedResourceTypes.InternetGateway,
        Common.Configuration.SupportedResourceTypes.NatGateway,
    ];
    
    public static IEnumerable<string> SecurityResourceTypes => [
        Common.Configuration.SupportedResourceTypes.SecurityGroup
    ];
    
    public static IEnumerable<string> IamResourceTypes =>
        ["iam.role", "iam.instanceprofile", "iam.user"];

    public static IEnumerable<string> ElbResourceTypes =>
    [
        Common.Configuration.SupportedResourceTypes.LoadBalancer,
        "elbv2.targetgroup",
        "elbv2.listener",
        "elbv2.loadbalancerlistener"
    ];
    
    public static IEnumerable<string> EcsResourceTypes => [
        Common.Configuration.SupportedResourceTypes.EcsService, 
        Common.Configuration.SupportedResourceTypes.EcsCluster, 
        Common.Configuration.SupportedResourceTypes.EcsTaskDefinition
    ];
    
    public static IEnumerable<string> ComputeResourceTypes =>
        ["ec2.instance", "ec2.volume", "lambda.function"];
}