namespace Infragraph.Common.Abstractions;

using Configuration;
using Models.ReactFlow;

/// <summary>
/// Orchestrates the complete diagram generation pipeline.
/// </summary>
public interface IDiagramPipeline
{
    /// <summary>
    /// Generates a React Flow diagram from Former2 JSON.
    /// </summary>
    /// <param name="jsonStream">The input stream containing Former2 JSON.</param>
    /// <param name="options">Diagram generation options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated React Flow diagram.</returns>
    Task<ReactFlowDiagram> GenerateAsync(
        Stream jsonStream,
        DiagramOptions? options = null,
        CancellationToken cancellationToken = default);
}
