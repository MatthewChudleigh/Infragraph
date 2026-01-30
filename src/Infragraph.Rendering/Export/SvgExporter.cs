namespace Infragraph.Rendering.Export;

using System.Globalization;
using System.Text;
using System.Web;
using Infragraph.Common.Models.Export;
using Infragraph.Common.Models.ReactFlow;

/// <summary>
/// Exports React Flow diagrams to SVG format.
/// </summary>
public sealed class SvgExporter
{
    private static readonly Dictionary<string, string> ServiceColors = new()
    {
        ["ec2"] = "#FF9900",
        ["ecs"] = "#FF9900",
        ["elbv2"] = "#8C4FFF",
        ["iam"] = "#DD4B39",
        ["s3"] = "#3F8624",
        ["rds"] = "#3B48CC",
        ["dynamodb"] = "#3B48CC",
        ["lambda"] = "#FF9900",
        ["sqs"] = "#FF4F8B",
        ["sns"] = "#FF4F8B",
        ["logs"] = "#FF4F8B",
        ["secretsmanager"] = "#DD4B39"
    };

    /// <summary>
    /// Exports a React Flow diagram to SVG.
    /// </summary>
    public string Export(ReactFlowDiagram diagram, ExportOptions options)
    {
        var bounds = CalculateBounds(diagram, options);
        var sb = new StringBuilder();

        // SVG header with proper namespace
        sb.AppendLine($@"<?xml version=""1.0"" encoding=""UTF-8""?>");
        sb.AppendLine($@"<svg xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink""");
        sb.AppendLine($@"     width=""{F(bounds.Width * options.Scale)}"" height=""{F(bounds.Height * options.Scale)}""");
        sb.AppendLine($@"     viewBox=""{F(bounds.MinX - options.Padding)} {F(bounds.MinY - options.Padding)} {F(bounds.Width)} {F(bounds.Height)}"">");

        // Styles
        AppendStyles(sb, options);

        // Defs for markers and gradients
        AppendDefs(sb);

        // Background
        sb.AppendLine($@"  <rect x=""{F(bounds.MinX - options.Padding)}"" y=""{F(bounds.MinY - options.Padding)}""");
        sb.AppendLine($@"        width=""{F(bounds.Width)}"" height=""{F(bounds.Height)}""");
        sb.AppendLine($@"        fill=""{options.BackgroundColor}""/>");

        // Title
        if (options.IncludeTitle)
        {
            var title = options.Title ?? $"AWS Infrastructure Diagram";
            sb.AppendLine($@"  <text x=""{F(bounds.MinX)}"" y=""{F(bounds.MinY - options.Padding / 2 + 5)}""");
            sb.AppendLine($@"        class=""diagram-title"">{Escape(title)}</text>");
        }

        // Render groups first (background containers)
        var groupNodes = diagram.Nodes.Where(n => n.Data.IsGroup).ToList();
        var resourceNodes = diagram.Nodes.Where(n => !n.Data.IsGroup).ToList();

        foreach (var group in groupNodes.OrderBy(g => GetGroupDepth(g, diagram.Nodes)))
        {
            RenderGroupNode(sb, group, options);
        }

        // Render edges
        foreach (var edge in diagram.Edges)
        {
            RenderEdge(sb, edge, diagram.Nodes, options);
        }

        // Render resource nodes
        foreach (var node in resourceNodes)
        {
            RenderResourceNode(sb, node, options);
        }

        // Metadata footer
        if (options.IncludeMetadata)
        {
            var footerY = bounds.MaxY + options.Padding / 2;
            var metadata = $"Resources: {diagram.Metadata.IncludedResources} | Relationships: {diagram.Metadata.TotalRelationships} | Generated: {diagram.Metadata.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC";
            sb.AppendLine($@"  <text x=""{F(bounds.MinX)}"" y=""{F(footerY)}""");
            sb.AppendLine($@"        class=""diagram-metadata"">{Escape(metadata)}</text>");
        }

        sb.AppendLine("</svg>");

        return sb.ToString();
    }

    private static void AppendStyles(StringBuilder sb, ExportOptions options)
    {
        sb.AppendLine("  <style>");
        sb.AppendLine($@"    .diagram-title {{ font-family: {options.FontFamily}; font-size: 18px; font-weight: 600; fill: #333; }}");
        sb.AppendLine($@"    .diagram-metadata {{ font-family: {options.FontFamily}; font-size: 11px; fill: #666; }}");
        sb.AppendLine($@"    .node-label {{ font-family: {options.FontFamily}; font-size: 12px; fill: #333; }}");
        sb.AppendLine($@"    .node-type {{ font-family: {options.FontFamily}; font-size: 10px; fill: #666; }}");
        sb.AppendLine($@"    .group-label {{ font-family: {options.FontFamily}; font-size: 11px; font-weight: 500; fill: #555; }}");
        sb.AppendLine($@"    .edge-label {{ font-family: {options.FontFamily}; font-size: 10px; fill: #666; }}");
        sb.AppendLine("  </style>");
    }

    private static void AppendDefs(StringBuilder sb)
    {
        sb.AppendLine("  <defs>");

        // Arrow markers for different relationship types
        var markerColors = new Dictionary<string, string>
        {
            ["default"] = "#666666",
            ["uses"] = "#4CAF50",
            ["attached"] = "#2196F3",
            ["references"] = "#FF5722",
            ["assumes"] = "#9C27B0",
            ["routes"] = "#FF9800",
            ["listens"] = "#8C4FFF",
            ["targets"] = "#00BCD4"
        };

        foreach (var (name, color) in markerColors)
        {
            sb.AppendLine($@"    <marker id=""arrow-{name}"" markerWidth=""10"" markerHeight=""7""");
            sb.AppendLine($@"            refX=""9"" refY=""3.5"" orient=""auto"" markerUnits=""strokeWidth"">");
            sb.AppendLine($@"      <polygon points=""0 0, 10 3.5, 0 7"" fill=""{color}""/>");
            sb.AppendLine("    </marker>");
        }

        // Drop shadow filter
        sb.AppendLine(@"    <filter id=""shadow"" x=""-20%"" y=""-20%"" width=""140%"" height=""140%"">");
        sb.AppendLine(@"      <feDropShadow dx=""1"" dy=""1"" stdDeviation=""2"" flood-opacity=""0.15""/>");
        sb.AppendLine("    </filter>");

        sb.AppendLine("  </defs>");
    }

    private static void RenderGroupNode(StringBuilder sb, ReactFlowNode node, ExportOptions options)
    {
        var x = node.Position.X;
        var y = node.Position.Y;
        var width = node.Width ?? 300;
        var height = node.Height ?? 200;

        var style = GetGroupStyleValues(node.Data.GroupType);

        sb.AppendLine($@"  <g class=""group-node"" id=""group-{Escape(node.Id)}"">");

        // Group container
        sb.AppendLine($@"    <rect x=""{F(x)}"" y=""{F(y)}"" width=""{F(width)}"" height=""{F(height)}""");
        sb.AppendLine($@"          rx=""{style.BorderRadius}"" ry=""{style.BorderRadius}""");
        sb.AppendLine($@"          fill=""{style.BackgroundColor}"" stroke=""{style.BorderColor}""");
        sb.AppendLine($@"          stroke-width=""{style.BorderWidth}"" stroke-dasharray=""{style.StrokeDasharray}""/>");

        // Group label
        sb.AppendLine($@"    <text x=""{F(x + 10)}"" y=""{F(y + 18)}"" class=""group-label"">{Escape(node.Data.Label)}</text>");

        sb.AppendLine("  </g>");
    }

    private static void RenderResourceNode(StringBuilder sb, ReactFlowNode node, ExportOptions options)
    {
        var x = node.Position.X;
        var y = node.Position.Y;
        var width = node.Width ?? 200;
        var height = node.Height ?? 60;

        var borderColor = GetServiceColor(node.Data.Service);

        sb.AppendLine($@"  <g class=""resource-node"" id=""node-{Escape(node.Id)}"">");

        // Node background with shadow
        sb.AppendLine($@"    <rect x=""{F(x)}"" y=""{F(y)}"" width=""{F(width)}"" height=""{F(height)}""");
        sb.AppendLine($@"          rx=""4"" ry=""4"" fill=""#ffffff"" stroke=""{borderColor}""");
        sb.AppendLine($@"          stroke-width=""2"" filter=""url(#shadow)""/>");

        // Service indicator bar
        sb.AppendLine($@"    <rect x=""{F(x)}"" y=""{F(y)}"" width=""4"" height=""{F(height)}""");
        sb.AppendLine($@"          rx=""4"" ry=""0"" fill=""{borderColor}""/>");

        // Node label
        var labelY = y + height / 2 - 6;
        sb.AppendLine($@"    <text x=""{F(x + 14)}"" y=""{F(labelY)}"" class=""node-label"">{Escape(TruncateLabel(node.Data.Label, 28))}</text>");

        // Resource type
        var typeY = y + height / 2 + 10;
        var resourceType = node.Data.ResourceType ?? "resource";
        sb.AppendLine($@"    <text x=""{F(x + 14)}"" y=""{F(typeY)}"" class=""node-type"">{Escape(resourceType)}</text>");

        sb.AppendLine("  </g>");
    }

    private static void RenderEdge(StringBuilder sb, ReactFlowEdge edge, List<ReactFlowNode> nodes, ExportOptions options)
    {
        var sourceNode = nodes.FirstOrDefault(n => n.Id == edge.Source);
        var targetNode = nodes.FirstOrDefault(n => n.Id == edge.Target);

        if (sourceNode == null || targetNode == null)
            return;

        // Calculate edge points (center of nodes for simplicity)
        var sourceX = sourceNode.Position.X + (sourceNode.Width ?? 200) / 2;
        var sourceY = sourceNode.Position.Y + (sourceNode.Height ?? 60) / 2;
        var targetX = targetNode.Position.X + (targetNode.Width ?? 200) / 2;
        var targetY = targetNode.Position.Y + (targetNode.Height ?? 60) / 2;

        // Adjust for node edges
        var (adjustedSourceX, adjustedSourceY) = AdjustPointToNodeEdge(
            sourceX, sourceY, targetX, targetY,
            sourceNode.Position.X, sourceNode.Position.Y,
            sourceNode.Width ?? 200, sourceNode.Height ?? 60);

        var (adjustedTargetX, adjustedTargetY) = AdjustPointToNodeEdge(
            targetX, targetY, sourceX, sourceY,
            targetNode.Position.X, targetNode.Position.Y,
            targetNode.Width ?? 200, targetNode.Height ?? 60);

        var edgeStyle = GetEdgeStyleValues(edge.Data?.RelationshipType);
        var markerId = GetMarkerId(edge.Data?.RelationshipType);

        sb.AppendLine($@"  <g class=""edge"" id=""edge-{Escape(edge.Id)}"">");

        // Edge path (smoothstep approximation)
        var path = CreateSmoothstepPath(adjustedSourceX, adjustedSourceY, adjustedTargetX, adjustedTargetY);
        sb.AppendLine($@"    <path d=""{path}"" fill=""none""");
        sb.AppendLine($@"          stroke=""{edgeStyle.Stroke}"" stroke-width=""{edgeStyle.StrokeWidth}""");
        if (!string.IsNullOrEmpty(edgeStyle.StrokeDasharray))
        {
            sb.Append($@"          stroke-dasharray=""{edgeStyle.StrokeDasharray}""");
        }
        sb.AppendLine($@" marker-end=""url(#{markerId})""/>");

        // Edge label
        if (options.IncludeEdgeLabels && !string.IsNullOrEmpty(edge.Label))
        {
            var midX = (adjustedSourceX + adjustedTargetX) / 2;
            var midY = (adjustedSourceY + adjustedTargetY) / 2;
            sb.AppendLine($@"    <rect x=""{F(midX - 30)}"" y=""{F(midY - 8)}"" width=""60"" height=""16""");
            sb.AppendLine($@"          fill=""white"" rx=""3"" ry=""3""/>");
            sb.AppendLine($@"    <text x=""{F(midX)}"" y=""{F(midY + 4)}"" text-anchor=""middle"" class=""edge-label"">{Escape(edge.Label)}</text>");
        }

        sb.AppendLine("  </g>");
    }

    private static string CreateSmoothstepPath(double x1, double y1, double x2, double y2)
    {
        // Create a smoothstep path (similar to React Flow)
        var midX = (x1 + x2) / 2;
        var midY = (y1 + y2) / 2;

        // Determine if path should be horizontal or vertical first
        var dx = Math.Abs(x2 - x1);
        var dy = Math.Abs(y2 - y1);

        if (dx > dy)
        {
            // Horizontal path with vertical step
            return $"M {F(x1)} {F(y1)} C {F(midX)} {F(y1)}, {F(midX)} {F(y2)}, {F(x2)} {F(y2)}";
        }
        else
        {
            // Vertical path with horizontal step
            return $"M {F(x1)} {F(y1)} C {F(x1)} {F(midY)}, {F(x2)} {F(midY)}, {F(x2)} {F(y2)}";
        }
    }

    private static (double x, double y) AdjustPointToNodeEdge(
        double pointX, double pointY, double otherX, double otherY,
        double nodeX, double nodeY, double nodeWidth, double nodeHeight)
    {
        // Calculate direction from point to other
        var dx = otherX - pointX;
        var dy = otherY - pointY;
        var distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance == 0) return (pointX, pointY);

        // Normalize
        dx /= distance;
        dy /= distance;

        // Find intersection with node boundary
        var halfWidth = nodeWidth / 2;
        var halfHeight = nodeHeight / 2;

        // Calculate scale factors to reach each edge
        var scaleX = dx != 0 ? halfWidth / Math.Abs(dx) : double.MaxValue;
        var scaleY = dy != 0 ? halfHeight / Math.Abs(dy) : double.MaxValue;

        var scale = Math.Min(scaleX, scaleY);

        return (pointX + dx * scale, pointY + dy * scale);
    }

    private static DiagramBounds CalculateBounds(ReactFlowDiagram diagram, ExportOptions options)
    {
        if (diagram.Nodes.Count == 0)
        {
            return new DiagramBounds(0, 0, 400, 300);
        }

        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;

        foreach (var node in diagram.Nodes)
        {
            var x = node.Position.X;
            var y = node.Position.Y;
            var width = node.Width ?? (node.Data.IsGroup ? 300 : 200);
            var height = node.Height ?? (node.Data.IsGroup ? 200 : 60);

            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x + width);
            maxY = Math.Max(maxY, y + height);
        }

        // Add padding and space for title/metadata
        var titleSpace = options.IncludeTitle ? 30 : 0;
        var metadataSpace = options.IncludeMetadata ? 25 : 0;

        return new DiagramBounds(
            minX - options.Padding,
            minY - options.Padding - titleSpace,
            maxX - minX + options.Padding * 2,
            maxY - minY + options.Padding * 2 + titleSpace + metadataSpace);
    }

    private static int GetGroupDepth(ReactFlowNode node, List<ReactFlowNode> allNodes)
    {
        var depth = 0;
        var current = node;

        while (current.ParentId != null)
        {
            depth++;
            current = allNodes.FirstOrDefault(n => n.Id == current.ParentId);
            if (current == null) break;
        }

        return depth;
    }

    private static string GetServiceColor(string? service)
    {
        if (service == null) return "#666666";
        return ServiceColors.GetValueOrDefault(service, "#666666");
    }

    private static GroupStyle GetGroupStyleValues(string? groupType)
    {
        return groupType switch
        {
            "vpc" => new GroupStyle("rgba(255, 153, 0, 0.05)", "#FF9900", 2, 8, "8 4"),
            "subnet" => new GroupStyle("rgba(255, 153, 0, 0.02)", "#FF9900", 1, 4, "4 2"),
            "ecs-cluster" => new GroupStyle("rgba(255, 153, 0, 0.08)", "#FF9900", 2, 8, ""),
            "load-balancer" => new GroupStyle("rgba(140, 79, 255, 0.05)", "#8C4FFF", 2, 8, ""),
            _ => new GroupStyle("rgba(128, 128, 128, 0.05)", "#888888", 1, 4, "4 2")
        };
    }

    private static EdgeStyle GetEdgeStyleValues(string? relationshipType)
    {
        return relationshipType?.ToLowerInvariant() switch
        {
            "belongsto" => new EdgeStyle("#888888", 1, "4 2"),
            "uses" => new EdgeStyle("#4CAF50", 2, ""),
            "attachedto" => new EdgeStyle("#2196F3", 2, ""),
            "references" => new EdgeStyle("#FF5722", 1, "2 2"),
            "assumes" => new EdgeStyle("#9C27B0", 2, ""),
            "routesto" => new EdgeStyle("#FF9800", 2, ""),
            "listensfor" => new EdgeStyle("#8C4FFF", 2, ""),
            "targets" => new EdgeStyle("#00BCD4", 2, ""),
            _ => new EdgeStyle("#666666", 1, "")
        };
    }

    private static string GetMarkerId(string? relationshipType)
    {
        return relationshipType?.ToLowerInvariant() switch
        {
            "uses" => "arrow-uses",
            "attachedto" => "arrow-attached",
            "references" => "arrow-references",
            "assumes" => "arrow-assumes",
            "routesto" => "arrow-routes",
            "listensfor" => "arrow-listens",
            "targets" => "arrow-targets",
            _ => "arrow-default"
        };
    }

    private static string TruncateLabel(string label, int maxLength)
    {
        if (label.Length <= maxLength) return label;
        return label[..(maxLength - 3)] + "...";
    }

    private static string Escape(string text) => HttpUtility.HtmlEncode(text);

    private static string F(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private sealed record DiagramBounds(double MinX, double MinY, double Width, double Height)
    {
        public double MaxX => MinX + Width;
        public double MaxY => MinY + Height;
    }

    private sealed record GroupStyle(string BackgroundColor, string BorderColor, int BorderWidth, int BorderRadius, string StrokeDasharray);

    private sealed record EdgeStyle(string Stroke, int StrokeWidth, string StrokeDasharray);
}
