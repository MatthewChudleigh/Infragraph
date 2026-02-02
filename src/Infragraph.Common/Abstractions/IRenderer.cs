namespace Infragraph.Common.Abstractions;

using Configuration;
using Models.Graph;

/// <summary>
/// Renders an infrastructure graph to a specific output format.
/// </summary>
/// <typeparam name="TOutput">The output type.</typeparam>
public interface IRenderer<TOutput>
{
    /// <summary>
    /// Renders the graph to the output format.
    /// </summary>
    /// <param name="graph">The infrastructure graph.</param>
    /// <param name="options">Diagram options.</param>
    /// <returns>The rendered output.</returns>
    TOutput Render(InfraGraph graph, DiagramOptions options);
}
