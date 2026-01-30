namespace Infragraph.Server.Configuration;

using Infragraph.Common.Abstractions;
using Infragraph.Common.Models.ReactFlow;
using Infragraph.Core.Graph;
using Infragraph.Core.Layout.Groupers;
using Infragraph.Core.Modeling;
using Infragraph.Core.Parsing;
using Infragraph.Core.Pipeline;
using Infragraph.Core.Relationships.Extractors;
using Infragraph.Rendering.Export;
using Infragraph.Rendering.ReactFlow;

/// <summary>
/// Extension methods for registering Infragraph services.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    /// Adds Infragraph services to the DI container.
    /// </summary>
    public static IServiceCollection AddInfragraphServices(this IServiceCollection services)
    {
        // Parser
        services.AddSingleton<IResourceParser, Former2Parser>();

        // Model factory
        services.AddSingleton<IResourceModelFactory, ResourceModelFactory>();

        // Relationship extractors
        services.AddSingleton<IRelationshipExtractor, VpcSubnetExtractor>();
        services.AddSingleton<IRelationshipExtractor, SecurityGroupExtractor>();
        services.AddSingleton<IRelationshipExtractor, EcsServiceExtractor>();
        services.AddSingleton<IRelationshipExtractor, ElbTargetGroupExtractor>();
        services.AddSingleton<IRelationshipExtractor, IamRoleExtractor>();
        services.AddSingleton<IRelationshipExtractor, ComputeExtractor>();

        // Grouping strategies
        services.AddSingleton<IGroupingStrategy, VpcGrouper>();
        services.AddSingleton<IGroupingStrategy, ServiceGrouper>();
        services.AddSingleton<IGroupingStrategy, AffinityGrouper>();
        services.AddSingleton<IGroupingStrategy, IamGrouper>();

        // Graph builder
        services.AddSingleton<IGraphBuilder, GraphBuilder>();

        // Renderer
        services.AddSingleton<IRenderer<ReactFlowDiagram>, ReactFlowRenderer>();

        // Exporters
        services.AddSingleton<SvgExporter>();
        services.AddSingleton<PngExporter>();

        // Pipeline orchestrator
        services.AddSingleton<IDiagramPipeline, DiagramPipeline>();

        return services;
    }
}
