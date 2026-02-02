namespace Infragraph.Core.Pipeline;

using Common.Abstractions;
using Common.Configuration;
using Common.Models.Domain;
using Common.Models.ReactFlow;

/// <summary>
/// Orchestrates the complete diagram generation pipeline.
/// </summary>
public sealed class DiagramPipeline(
    IResourceParser parser,
    IGraphBuilder graphBuilder,
    IResourceModelFactory modelFactory,
    IEnumerable<IRelationshipExtractor> extractors,
    IRenderer<ReactFlowDiagram> renderer)
    : IDiagramPipeline
{
    public async Task<ReactFlowDiagram> GenerateAsync(
        Stream jsonStream,
        DiagramOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= DiagramOptions.Default;

        // Step 1: Parse Former2 JSON
        var former2Resources = new List<Common.Models.Former2.Former2Resource>();
        await foreach (var resource in parser.ParseAsync(jsonStream, cancellationToken))
        {
            former2Resources.Add(resource);
        }

        // Step 2: Convert to domain models
        var resources = former2Resources.Select(modelFactory.CreateModel).ToList();

        // Step 3: Build resource index for relationship extraction (handle duplicates)
        var resourceIndex = new Dictionary<string, AwsResource>();
        foreach (var resource in resources)
        {
            resourceIndex.TryAdd(resource.Id, resource);
        }

        // Also index by common ID formats (VpcId, SubnetId, etc.)
        AddAlternativeIds(resourceIndex, resources);

        // Step 4: Extract relationships
        var relationships = new List<ResourceRelationship>();
        foreach (var resource in resources)
        {
            foreach (var extractor in extractors)
            {
                if (extractor.SupportedResourceTypes.Contains(resource.Type))
                {
                    relationships.AddRange(extractor.ExtractRelationships(resource, resourceIndex));
                }
            }
        }

        // Step 5: Build graph
        var graph = graphBuilder.BuildGraph(resources, relationships, options);

        // Step 6: Render to React Flow format
        var diagram = renderer.Render(graph, options);

        return diagram;
    }

    /// <summary>
    /// Adds alternative IDs to the resource index for easier lookup.
    /// </summary>
    private static void AddAlternativeIds(Dictionary<string, AwsResource> index, List<AwsResource> resources)
    {
        foreach (var resource in resources)
        {
            // Add by ARN if different from ID
            if (!string.IsNullOrEmpty(resource.Arn) && resource.Arn != resource.Id)
            {
                index.TryAdd(resource.Arn, resource);
            }

            // Add by resource-specific IDs
            switch (resource)
            {
                case VpcResource vpc when !string.IsNullOrEmpty(vpc.VpcId):
                    index.TryAdd(vpc.VpcId, resource);
                    break;
                case SubnetResource subnet when !string.IsNullOrEmpty(subnet.SubnetId):
                    index.TryAdd(subnet.SubnetId, resource);
                    break;
                case SecurityGroupResource sg when !string.IsNullOrEmpty(sg.GroupId):
                    index.TryAdd(sg.GroupId, resource);
                    break;
                case RouteTableResource rt when !string.IsNullOrEmpty(rt.RouteTableId):
                    index.TryAdd(rt.RouteTableId, resource);
                    break;
                case InternetGatewayResource igw when !string.IsNullOrEmpty(igw.InternetGatewayId):
                    index.TryAdd(igw.InternetGatewayId, resource);
                    break;
                case NatGatewayResource nat when !string.IsNullOrEmpty(nat.NatGatewayId):
                    index.TryAdd(nat.NatGatewayId, resource);
                    break;
                case TransitGatewayResource tgw when !string.IsNullOrEmpty(tgw.TransitGatewayId):
                    index.TryAdd(tgw.TransitGatewayId, resource);
                    break;
                case Ec2InstanceResource ec2 when !string.IsNullOrEmpty(ec2.InstanceId):
                    index.TryAdd(ec2.InstanceId, resource);
                    break;
                case EcsClusterResource cluster when !string.IsNullOrEmpty(cluster.ClusterArn):
                    index.TryAdd(cluster.ClusterArn, resource);
                    break;
                case EcsServiceResource svc when !string.IsNullOrEmpty(svc.ServiceArn):
                    index.TryAdd(svc.ServiceArn, resource);
                    break;
                case EcsTaskDefinitionResource td when !string.IsNullOrEmpty(td.TaskDefinitionArn):
                    index.TryAdd(td.TaskDefinitionArn, resource);
                    break;
                case LoadBalancerResource lb when !string.IsNullOrEmpty(lb.LoadBalancerArn):
                    index.TryAdd(lb.LoadBalancerArn, resource);
                    break;
                case TargetGroupResource tg when !string.IsNullOrEmpty(tg.TargetGroupArn):
                    index.TryAdd(tg.TargetGroupArn, resource);
                    break;
                case ListenerResource listener when !string.IsNullOrEmpty(listener.ListenerArn):
                    index.TryAdd(listener.ListenerArn, resource);
                    break;
                case IamRoleResource role when !string.IsNullOrEmpty(role.RoleArn):
                    index.TryAdd(role.RoleArn, resource);
                    break;
                case InstanceProfileResource profile when !string.IsNullOrEmpty(profile.InstanceProfileArn):
                    index.TryAdd(profile.InstanceProfileArn, resource);
                    break;
                case IamPolicyResource policy when !string.IsNullOrEmpty(policy.PolicyArn):
                    index.TryAdd(policy.PolicyArn, resource);
                    break;
                case LambdaFunctionResource lambda when !string.IsNullOrEmpty(lambda.FunctionArn):
                    index.TryAdd(lambda.FunctionArn, resource);
                    break;
            }
        }
    }
}
