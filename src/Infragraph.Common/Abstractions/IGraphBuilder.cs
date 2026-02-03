namespace Infragraph.Common.Abstractions;

using Configuration;
using Models.Domain;
using Models.Graph;

/// <summary>
/// Builds a graph model from AWS resources and their relationships.
/// </summary>
public interface IGraphBuilder
{
    /// <summary>
    /// Builds an infrastructure graph from resources and relationships.
    /// </summary>
    /// <param name="resourceSet">The set of AWS resources and relationships between resources.</param>
    /// <param name="options">Diagram generation options.</param>
    /// <returns>The constructed infrastructure graph.</returns>
    InfraGraph BuildGraph(
        ResourceSet resourceSet,
        DiagramOptions options);
}
