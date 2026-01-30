namespace Infragraph.Server.Endpoints;

using System.Text.Json;
using Infragraph.Common.Models.Export;
using Infragraph.Common.Models.ReactFlow;
using Infragraph.Rendering.Export;

/// <summary>
/// API endpoints for exporting diagrams.
/// </summary>
public static class ExportEndpoints
{
    /// <summary>
    /// Maps export-related endpoints.
    /// </summary>
    public static RouteGroupBuilder MapExportEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/export/svg", ExportSvg)
            .WithName("ExportSvg")
            .WithDescription("Exports a positioned diagram as SVG")
            .Accepts<ExportRequest>("application/json")
            .Produces(StatusCodes.Status200OK, contentType: "image/svg+xml")
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/export/png", ExportPng)
            .WithName("ExportPng")
            .WithDescription("Exports a positioned diagram as PNG")
            .Accepts<ExportRequest>("application/json")
            .Produces(StatusCodes.Status200OK, contentType: "image/png")
            .Produces(StatusCodes.Status400BadRequest);

        return group;
    }

    private static async Task<IResult> ExportSvg(
        HttpRequest request,
        SvgExporter svgExporter,
        CancellationToken cancellationToken)
    {
        try
        {
            var exportRequest = await ParseExportRequest(request.Body, cancellationToken);

            if (exportRequest.Diagram.Nodes.Count == 0)
            {
                return Results.BadRequest(new { error = "Diagram has no nodes to export" });
            }

            var svg = svgExporter.Export(exportRequest.Diagram, exportRequest.Options);
            var bytes = System.Text.Encoding.UTF8.GetBytes(svg);

            var fileName = GenerateFileName(exportRequest.Options.Title, "svg");

            return Results.File(
                bytes,
                contentType: "image/svg+xml",
                fileDownloadName: fileName);
        }
        catch (JsonException ex)
        {
            return Results.BadRequest(new { error = "Invalid JSON", details = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = "Export failed", details = ex.Message });
        }
    }

    private static async Task<IResult> ExportPng(
        HttpRequest request,
        PngExporter pngExporter,
        CancellationToken cancellationToken)
    {
        try
        {
            var exportRequest = await ParseExportRequest(request.Body, cancellationToken);

            if (exportRequest.Diagram.Nodes.Count == 0)
            {
                return Results.BadRequest(new { error = "Diagram has no nodes to export" });
            }

            var png = pngExporter.Export(exportRequest.Diagram, exportRequest.Options);
            var fileName = GenerateFileName(exportRequest.Options.Title, "png");

            return Results.File(
                png,
                contentType: "image/png",
                fileDownloadName: fileName);
        }
        catch (JsonException ex)
        {
            return Results.BadRequest(new { error = "Invalid JSON", details = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = "Export failed", details = ex.Message });
        }
    }

    private static async Task<ExportRequest> ParseExportRequest(Stream body, CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var request = await JsonSerializer.DeserializeAsync<ExportRequest>(body, options, cancellationToken);

        if (request == null)
        {
            throw new JsonException("Failed to parse export request");
        }

        return request;
    }

    private static string GenerateFileName(string? title, string extension)
    {
        var baseName = string.IsNullOrWhiteSpace(title)
            ? "aws-infrastructure-diagram"
            : SanitizeFileName(title);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        return $"{baseName}-{timestamp}.{extension}";
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName
            .Where(c => !invalidChars.Contains(c))
            .ToArray());

        // Replace spaces with hyphens and convert to lowercase
        return sanitized
            .Replace(' ', '-')
            .ToLowerInvariant();
    }
}
