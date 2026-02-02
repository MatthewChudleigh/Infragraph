using Infragraph.Common.Abstractions;
using Infragraph.Common.Models.Domain;

namespace Infragraph.Core.Relationships;

/// <summary>
/// Extracts ELB/ALB relationships (load balancer, target group, listener).
/// </summary>
public sealed class ElbTargetGroupExtractor : IRelationshipExtractor
{
    public IEnumerable<string> SupportedResourceTypes =>
        [
            Common.Configuration.SupportedResourceTypes.LoadBalancer, 
            "elbv2.targetgroup", 
            "elbv2.listener", 
            "elbv2.loadbalancerlistener"
        ];

    public IEnumerable<ResourceRelationship> ExtractRelationships(
        IReadOnlyDictionary<string, AwsResource> index,
        AwsResource source)
    {
        switch (source)
        {
            case LoadBalancerResource lb:
                foreach (var rel in ExtractLoadBalancerRelationships(lb, index))
                    yield return rel;
                break;

            case TargetGroupResource tg:
                foreach (var rel in ExtractTargetGroupRelationships(tg, index))
                    yield return rel;
                break;

            case ListenerResource listener:
                foreach (var rel in ExtractListenerRelationships(listener, index))
                    yield return rel;
                break;
        }
    }

    private static IEnumerable<ResourceRelationship> ExtractLoadBalancerRelationships(
        LoadBalancerResource lb,
        IReadOnlyDictionary<string, AwsResource> index)
    {
        // Load balancer in VPC
        var vpc = FindResourceByProperty<VpcResource>(index, r => r.VpcId == lb.VpcId || r.Id == lb.VpcId);
        if (vpc != null)
        {
            yield return new ResourceRelationship
            {
                SourceId = lb.Id,
                TargetId = vpc.Id,
                RelationshipType = RelationshipType.BelongsTo,
                Label = "in VPC"
            };
        }

        // Load balancer uses subnets
        foreach (var subnetId in lb.SubnetIds)
        {
            if (index.TryGetValue(subnetId, out var subnet))
            {
                yield return new ResourceRelationship
                {
                    SourceId = lb.Id,
                    TargetId = subnet.Id,
                    RelationshipType = RelationshipType.Uses,
                    Label = "uses"
                };
            }
        }

        // Load balancer uses security groups
        foreach (var sgId in lb.SecurityGroupIds)
        {
            if (index.TryGetValue(sgId, out var sg))
            {
                yield return new ResourceRelationship
                {
                    SourceId = lb.Id,
                    TargetId = sg.Id,
                    RelationshipType = RelationshipType.Uses,
                    Label = "uses"
                };
            }
        }
    }

    private static IEnumerable<ResourceRelationship> ExtractTargetGroupRelationships(
        TargetGroupResource tg,
        IReadOnlyDictionary<string, AwsResource> index)
    {
        // Target group in VPC
        if (!string.IsNullOrEmpty(tg.VpcId))
        {
            var vpc = FindResourceByProperty<VpcResource>(index, r => r.VpcId == tg.VpcId || r.Id == tg.VpcId);
            if (vpc != null)
            {
                yield return new ResourceRelationship
                {
                    SourceId = tg.Id,
                    TargetId = vpc.Id,
                    RelationshipType = RelationshipType.BelongsTo,
                    Label = "in VPC"
                };
            }
        }

        // Target group attached to load balancers
        foreach (var lbArn in tg.LoadBalancerArns)
        {
            var lb = FindResourceByArn(index, lbArn);
            if (lb != null)
            {
                yield return new ResourceRelationship
                {
                    SourceId = tg.Id,
                    TargetId = lb.Id,
                    RelationshipType = RelationshipType.AttachedTo,
                    Label = "registered with"
                };
            }
        }
    }

    private static IEnumerable<ResourceRelationship> ExtractListenerRelationships(
        ListenerResource listener,
        IReadOnlyDictionary<string, AwsResource> index)
    {
        // Listener belongs to load balancer
        var lb = FindResourceByArn(index, listener.LoadBalancerArn);
        if (lb != null)
        {
            yield return new ResourceRelationship
            {
                SourceId = listener.Id,
                TargetId = lb.Id,
                RelationshipType = RelationshipType.BelongsTo,
                Label = $"{listener.Protocol}:{listener.Port}"
            };

            // Also create ListensFor relationship
            yield return new ResourceRelationship
            {
                SourceId = listener.Id,
                TargetId = lb.Id,
                RelationshipType = RelationshipType.ListensFor,
                Label = "listens"
            };
        }

        // Listener forwards to target groups
        foreach (var action in listener.DefaultActions)
        {
            if (action.Type == "forward" && !string.IsNullOrEmpty(action.TargetGroupArn))
            {
                var tg = FindResourceByArn(index, action.TargetGroupArn);
                if (tg != null)
                {
                    yield return new ResourceRelationship
                    {
                        SourceId = listener.Id,
                        TargetId = tg.Id,
                        RelationshipType = RelationshipType.Targets,
                        Label = "forwards to"
                    };
                }
            }
        }
    }

    private static T? FindResourceByProperty<T>(
        IReadOnlyDictionary<string, AwsResource> index,
        Func<T, bool> predicate) where T : AwsResource
    {
        return index.Values.OfType<T>().FirstOrDefault(predicate);
    }

    private static AwsResource? FindResourceByArn(
        IReadOnlyDictionary<string, AwsResource> index,
        string? arn)
    {
        if (string.IsNullOrEmpty(arn))
            return null;

        if (index.TryGetValue(arn, out var resource))
            return resource;

        return index.Values.FirstOrDefault(r => r.Arn == arn);
    }
}
