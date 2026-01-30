namespace Infragraph.Server.Endpoints;

using System.Text.Json;
using Infragraph.Common.Abstractions;
using Infragraph.Common.Configuration;
using Infragraph.Common.Models.ReactFlow;

/// <summary>
/// API endpoints for diagram generation.
/// </summary>
public static class DiagramEndpoints
{
    /// <summary>
    /// Maps diagram-related endpoints.
    /// </summary>
    public static RouteGroupBuilder MapDiagramEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/diagram", GenerateDiagram)
            .WithName("GenerateDiagram")
            .WithDescription("Generates a React Flow diagram from Former2 JSON")
            .Accepts<Stream>("application/json")
            .Produces<ReactFlowDiagram>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/diagram/analyze", AnalyzeDiagram)
            .WithName("AnalyzeDiagram")
            .WithDescription("Analyzes Former2 JSON and returns resource/relationship statistics")
            .Accepts<Stream>("application/json")
            .Produces<AnalysisResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        return group;
    }

    private static async Task<IResult> GenerateDiagram(
        HttpRequest request,
        IDiagramPipeline pipeline,
        CancellationToken cancellationToken)
    {
        try
        {
            // Parse options from query string
            var options = ParseDiagramOptions(request.Query);

            // Generate diagram from request body
            var diagram = await pipeline.GenerateAsync(
                request.Body,
                options,
                cancellationToken);

            return Results.Ok(diagram);
        }
        catch (JsonException ex)
        {
            return Results.BadRequest(new { error = "Invalid JSON", details = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = "Invalid Former2 format", details = ex.Message });
        }
    }

    private static async Task<IResult> AnalyzeDiagram(
        HttpRequest request,
        IDiagramPipeline pipeline,
        CancellationToken cancellationToken)
    {
        try
        {
            // Generate full diagram to get analysis
            var options = new DiagramOptions { ShowIsolatedNodes = true };
            var diagram = await pipeline.GenerateAsync(
                request.Body,
                options,
                cancellationToken);

            // Build analysis result
            var analysis = new AnalysisResult
            {
                TotalResources = diagram.Metadata.TotalResources,
                IncludedResources = diagram.Metadata.IncludedResources,
                TotalRelationships = diagram.Metadata.TotalRelationships,
                ResourceTypes = diagram.Metadata.ResourceTypes,
                Regions = diagram.Metadata.Regions,
                NodeCount = diagram.Nodes.Count,
                EdgeCount = diagram.Edges.Count,
                ResourceTypeCounts = diagram.Nodes
                    .Where(n => !string.IsNullOrEmpty(n.Data.ResourceType))
                    .GroupBy(n => n.Data.ResourceType!)
                    .ToDictionary(g => g.Key, g => g.Count()),
                RelationshipTypeCounts = diagram.Edges
                    .Where(e => e.Data?.RelationshipType != null)
                    .GroupBy(e => e.Data!.RelationshipType!)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return Results.Ok(analysis);
        }
        catch (JsonException ex)
        {
            return Results.BadRequest(new { error = "Invalid JSON", details = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = "Invalid Former2 format", details = ex.Message });
        }
    }

    private static DiagramOptions ParseDiagramOptions(IQueryCollection query)
    {
        var options = new DiagramOptions();

        if (query.TryGetValue("includeTypes", out var includeTypes))
        {
            options = options with
            {
                IncludeTypes = includeTypes.ToString().Split(',').ToHashSet()
            };
        }

        if (query.TryGetValue("excludeTypes", out var excludeTypes))
        {
            options = options with
            {
                ExcludeTypes = excludeTypes.ToString().Split(',').ToHashSet()
            };
        }

        if (query.TryGetValue("regions", out var regions))
        {
            options = options with
            {
                IncludeRegions = regions.ToString().Split(',').ToHashSet()
            };
        }

        if (query.TryGetValue("showIsolated", out var showIsolated))
        {
            options = options with
            {
                ShowIsolatedNodes = bool.TryParse(showIsolated, out var show) && show
            };
        }

        if (query.TryGetValue("grouping", out var grouping))
        {
            options = options with
            {
                GroupingStrategies = grouping.ToString().Split(',').ToList()
            };
        }

        return options;
    }
}

/// <summary>
/// Result of diagram analysis.
/// </summary>
public sealed class AnalysisResult
{
    public int TotalResources { get; init; }
    public int IncludedResources { get; init; }
    public int TotalRelationships { get; init; }
    public List<string> ResourceTypes { get; init; } = [];
    public List<string> Regions { get; init; } = [];
    public int NodeCount { get; init; }
    public int EdgeCount { get; init; }
    public Dictionary<string, int> ResourceTypeCounts { get; init; } = new();
    public Dictionary<string, int> RelationshipTypeCounts { get; init; } = new();
}
