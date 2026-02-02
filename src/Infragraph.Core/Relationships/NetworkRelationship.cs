using Infragraph.Common.Abstractions;
using Infragraph.Common.Models.Domain;

namespace Infragraph.Core.Relationships;

/// <summary>
/// Extracts VPC-to-Subnet containment relationships and subnet-to-VPC belonging relationships.
/// </summary>
public sealed class NetworkRelationship : IRelationshipExtractor
{
    public IEnumerable<string> SupportedResourceTypes => [
        "ec2.vpc", 
        "ec2.subnet"
    ];

    public IEnumerable<ResourceRelationship> ExtractRelationships(
        IReadOnlyDictionary<string, AwsResource> index,
        AwsResource source)
    {
        switch (source)
        {
            case SubnetResource subnet:
                foreach (var rel in ExtractSubnetRelationships(subnet, index))
                    yield return rel;
                break;

            case SecurityGroupResource sg:
                foreach (var rel in ExtractSecurityGroupVpcRelationship(sg, index))
                    yield return rel;
                break;

            case RouteTableResource rt:
                foreach (var rel in ExtractRouteTableRelationships(rt, index))
                    yield return rel;
                break;

            case InternetGatewayResource igw:
                foreach (var rel in ExtractInternetGatewayRelationships(igw, index))
                    yield return rel;
                break;

            case NatGatewayResource nat:
                foreach (var rel in ExtractNatGatewayRelationships(nat, index))
                    yield return rel;
                break;
        }
    }

    private static IEnumerable<ResourceRelationship> ExtractSubnetRelationships(
        SubnetResource subnet,
        IReadOnlyDictionary<string, AwsResource> index)
    {
        // Find VPC by VpcId
        var vpc = FindResourceByProperty<VpcResource>(index, r => r.VpcId == subnet.VpcId || r.Id == subnet.VpcId);
        if (vpc == null) yield break;
        
        // Subnet belongs to VPC
        yield return new ResourceRelationship
        {
            SourceId = subnet.Id,
            TargetId = vpc.Id,
            RelationshipType = RelationshipType.BelongsTo,
            Label = "in VPC"
        };

        // VPC contains Subnet (reverse relationship)
        yield return new ResourceRelationship
        {
            SourceId = vpc.Id,
            TargetId = subnet.Id,
            RelationshipType = RelationshipType.Contains,
            Label = "contains"
        };
    }

    private static IEnumerable<ResourceRelationship> ExtractSecurityGroupVpcRelationship(
        SecurityGroupResource sg,
        IReadOnlyDictionary<string, AwsResource> index)
    {
        var vpc = FindResourceByProperty<VpcResource>(index, r => r.VpcId == sg.VpcId || r.Id == sg.VpcId);
        if (vpc != null)
        {
            yield return new ResourceRelationship
            {
                SourceId = sg.Id,
                TargetId = vpc.Id,
                RelationshipType = RelationshipType.BelongsTo,
                Label = "in VPC"
            };
        }
    }

    private static IEnumerable<ResourceRelationship> ExtractRouteTableRelationships(
        RouteTableResource rt,
        IReadOnlyDictionary<string, AwsResource> index)
    {
        // Route table belongs to VPC
        var vpc = FindResourceByProperty<VpcResource>(index, r => r.VpcId == rt.VpcId || r.Id == rt.VpcId);
        if (vpc != null)
        {
            yield return new ResourceRelationship
            {
                SourceId = rt.Id,
                TargetId = vpc.Id,
                RelationshipType = RelationshipType.BelongsTo,
                Label = "in VPC"
            };
        }

        // Route table associated with subnets
        foreach (var subnetId in rt.AssociatedSubnetIds)
        {
            if (index.TryGetValue(subnetId, out var subnet))
            {
                yield return new ResourceRelationship
                {
                    SourceId = rt.Id,
                    TargetId = subnet.Id,
                    RelationshipType = RelationshipType.AttachedTo,
                    Label = "routes for"
                };
            }
        }

        // Routes to gateways
        foreach (var route in rt.Routes)
        {
            if (!string.IsNullOrEmpty(route.GatewayId) && route.GatewayId != "local")
            {
                if (index.TryGetValue(route.GatewayId, out var gw))
                {
                    yield return new ResourceRelationship
                    {
                        SourceId = rt.Id,
                        TargetId = gw.Id,
                        RelationshipType = RelationshipType.RoutesTo,
                        Label = route.DestinationCidrBlock
                    };
                }
            }

            if (string.IsNullOrEmpty(route.NatGatewayId)) continue;
            
            if (index.TryGetValue(route.NatGatewayId, out var nat))
            {
                yield return new ResourceRelationship
                {
                    SourceId = rt.Id,
                    TargetId = nat.Id,
                    RelationshipType = RelationshipType.RoutesTo,
                    Label = route.DestinationCidrBlock
                };
            }
        }
    }

    private static IEnumerable<ResourceRelationship> ExtractInternetGatewayRelationships(
        InternetGatewayResource igw,
        IReadOnlyDictionary<string, AwsResource> index)
    {
        return igw.AttachedVpcIds.Select(vpcId => 
            FindResourceByProperty<VpcResource>(index, r => r.VpcId == vpcId || r.Id == vpcId))
            .OfType<VpcResource>()
            .Select(vpc => new ResourceRelationship
        {
            SourceId = igw.Id,
            TargetId = vpc.Id,
            RelationshipType = RelationshipType.AttachedTo,
            Label = "attached to"
        });
    }

    private static IEnumerable<ResourceRelationship> ExtractNatGatewayRelationships(
        NatGatewayResource nat,
        IReadOnlyDictionary<string, AwsResource> index)
    {
        // NAT Gateway in subnet
        if (string.IsNullOrEmpty(nat.SubnetId)) yield break;
        
        if (index.TryGetValue(nat.SubnetId, out var subnet))
        {
            yield return new ResourceRelationship
            {
                SourceId = nat.Id,
                TargetId = subnet.Id,
                RelationshipType = RelationshipType.BelongsTo,
                Label = "in subnet"
            };
        }
    }

    private static T? FindResourceByProperty<T>(
        IReadOnlyDictionary<string, AwsResource> index,
        Func<T, bool> predicate) where T : AwsResource
    {
        return index.Values.OfType<T>().FirstOrDefault(predicate);
    }
}
