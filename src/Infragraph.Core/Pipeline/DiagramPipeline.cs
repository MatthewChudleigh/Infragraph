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
        await foreach (var result in parser.ParseAsync(jsonStream, cancellationToken))
        {
            if (result.Result(out var resource, out _))
            {
                former2Resources.Add(resource);
            }
        }

        // Step 2: Convert to domain models
        var resourceSet = modelFactory.CreateResourceSet(former2Resources);

        // Step 5: Build graph
        var graph = graphBuilder.BuildGraph( resourceSet, options);

        // Step 6: Render to React Flow format
        var diagram = renderer.Render(graph, options);

        return diagram;
    }
}
