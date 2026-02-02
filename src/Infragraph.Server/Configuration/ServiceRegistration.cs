using Infragraph.Core.Graph.Groupers;
using Infragraph.Core.Relationships;

namespace Infragraph.Server.Configuration;

using Common.Abstractions;
using Common.Models.ReactFlow;
using Core.Graph;
using Core.Modeling;
using Core.Parsing;
using Core.Pipeline;
using Rendering.Export;
using Rendering.ReactFlow;

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
        services.AddSingleton<IRelationshipExtractor, NetworkRelationship>();
        services.AddSingleton<IRelationshipExtractor, SecurityRelationship>();
        services.AddSingleton<IRelationshipExtractor, EcsRelationship>();
        services.AddSingleton<IRelationshipExtractor, ElbRelationship>();
        services.AddSingleton<IRelationshipExtractor, IamRelationship>();
        services.AddSingleton<IRelationshipExtractor, ComputeRelationship>();

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
