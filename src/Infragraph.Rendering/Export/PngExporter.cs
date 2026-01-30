namespace Infragraph.Rendering.Export;

using Infragraph.Common.Models.Export;
using Infragraph.Common.Models.ReactFlow;
using SkiaSharp;

/// <summary>
/// Exports React Flow diagrams to PNG format using SkiaSharp.
/// </summary>
public sealed class PngExporter
{
    private static readonly Dictionary<string, SKColor> ServiceColors = new()
    {
        ["ec2"] = SKColor.Parse("#FF9900"),
        ["ecs"] = SKColor.Parse("#FF9900"),
        ["elbv2"] = SKColor.Parse("#8C4FFF"),
        ["iam"] = SKColor.Parse("#DD4B39"),
        ["s3"] = SKColor.Parse("#3F8624"),
        ["rds"] = SKColor.Parse("#3B48CC"),
        ["dynamodb"] = SKColor.Parse("#3B48CC"),
        ["lambda"] = SKColor.Parse("#FF9900"),
        ["sqs"] = SKColor.Parse("#FF4F8B"),
        ["sns"] = SKColor.Parse("#FF4F8B"),
        ["logs"] = SKColor.Parse("#FF4F8B"),
        ["secretsmanager"] = SKColor.Parse("#DD4B39")
    };

    private static readonly Dictionary<string, SKColor> EdgeColors = new()
    {
        ["belongsto"] = SKColor.Parse("#888888"),
        ["uses"] = SKColor.Parse("#4CAF50"),
        ["attachedto"] = SKColor.Parse("#2196F3"),
        ["references"] = SKColor.Parse("#FF5722"),
        ["assumes"] = SKColor.Parse("#9C27B0"),
        ["routesto"] = SKColor.Parse("#FF9800"),
        ["listensfor"] = SKColor.Parse("#8C4FFF"),
        ["targets"] = SKColor.Parse("#00BCD4")
    };

    /// <summary>
    /// Exports a React Flow diagram to PNG bytes.
    /// </summary>
    public byte[] Export(ReactFlowDiagram diagram, ExportOptions options)
    {
        var bounds = CalculateBounds(diagram, options);

        var width = (int)(bounds.Width * options.Scale);
        var height = (int)(bounds.Height * options.Scale);

        // Ensure minimum size
        width = Math.Max(width, 100);
        height = Math.Max(height, 100);

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;

        // Apply scale
        canvas.Scale((float)options.Scale);

        // Translate to account for bounds offset
        canvas.Translate(-(float)(bounds.MinX - options.Padding), -(float)(bounds.MinY - options.Padding));

        // Draw background
        var bgColor = ParseColor(options.BackgroundColor);
        canvas.Clear(bgColor);

        // Draw title
        if (options.IncludeTitle)
        {
            var title = options.Title ?? "AWS Infrastructure Diagram";
            DrawTitle(canvas, title, bounds, options);
        }

        // Draw groups first (background containers)
        var groupNodes = diagram.Nodes.Where(n => n.Data.IsGroup).ToList();
        var resourceNodes = diagram.Nodes.Where(n => !n.Data.IsGroup).ToList();

        foreach (var group in groupNodes.OrderBy(g => GetGroupDepth(g, diagram.Nodes)))
        {
            DrawGroupNode(canvas, group, options);
        }

        // Draw edges
        foreach (var edge in diagram.Edges)
        {
            DrawEdge(canvas, edge, diagram.Nodes, options);
        }

        // Draw resource nodes
        foreach (var node in resourceNodes)
        {
            DrawResourceNode(canvas, node, options);
        }

        // Draw metadata footer
        if (options.IncludeMetadata)
        {
            DrawMetadata(canvas, diagram.Metadata, bounds, options);
        }

        // Encode to PNG
        using var image = surface.Snapshot();
        var quality = (int)(options.Quality * 100);
        using var data = image.Encode(SKEncodedImageFormat.Png, quality);

        return data.ToArray();
    }

    private static void DrawTitle(SKCanvas canvas, string title, DiagramBounds bounds, ExportOptions options)
    {
        using var font = new SKFont(
            SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
            18);
        using var paint = new SKPaint
        {
            Color = SKColors.Black.WithAlpha(200),
            IsAntialias = true
        };

        var x = (float)(bounds.MinX);
        var y = (float)(bounds.MinY - options.Padding / 2 + 5);

        canvas.DrawText(title, x, y, SKTextAlign.Left, font, paint);
    }

    private static void DrawMetadata(SKCanvas canvas, ReactFlowMetadata metadata, DiagramBounds bounds, ExportOptions options)
    {
        using var font = new SKFont(SKTypeface.FromFamilyName("Arial"), 11);
        using var paint = new SKPaint
        {
            Color = SKColors.Gray,
            IsAntialias = true
        };

        var text = $"Resources: {metadata.IncludedResources} | Relationships: {metadata.TotalRelationships} | Generated: {metadata.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC";
        var x = (float)(bounds.MinX);
        var y = (float)(bounds.MaxY + options.Padding / 2);

        canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
    }

    private static void DrawGroupNode(SKCanvas canvas, ReactFlowNode node, ExportOptions options)
    {
        var x = (float)node.Position.X;
        var y = (float)node.Position.Y;
        var width = (float)(node.Width ?? 300);
        var height = (float)(node.Height ?? 200);

        var style = GetGroupStyle(node.Data.GroupType);
        var rect = new SKRect(x, y, x + width, y + height);

        // Draw background
        using var bgPaint = new SKPaint
        {
            Color = style.BackgroundColor,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        canvas.DrawRoundRect(rect, style.BorderRadius, style.BorderRadius, bgPaint);

        // Draw border
        using var borderPaint = new SKPaint
        {
            Color = style.BorderColor,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = style.BorderWidth,
            IsAntialias = true
        };

        if (style.IsDashed)
        {
            borderPaint.PathEffect = SKPathEffect.CreateDash(style.DashPattern, 0);
        }

        canvas.DrawRoundRect(rect, style.BorderRadius, style.BorderRadius, borderPaint);

        // Draw label
        using var font = new SKFont(
            SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Medium, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
            11);
        using var textPaint = new SKPaint
        {
            Color = SKColor.Parse("#555555"),
            IsAntialias = true
        };

        canvas.DrawText(node.Data.Label, x + 10, y + 18, SKTextAlign.Left, font, textPaint);
    }

    private static void DrawResourceNode(SKCanvas canvas, ReactFlowNode node, ExportOptions options)
    {
        var x = (float)node.Position.X;
        var y = (float)node.Position.Y;
        var width = (float)(node.Width ?? 200);
        var height = (float)(node.Height ?? 60);

        var borderColor = GetServiceColor(node.Data.Service);
        var rect = new SKRect(x, y, x + width, y + height);

        // Draw shadow
        using var shadowPaint = new SKPaint
        {
            Color = SKColors.Black.WithAlpha(25),
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 2)
        };
        var shadowRect = new SKRect(x + 1, y + 1, x + width + 1, y + height + 1);
        canvas.DrawRoundRect(shadowRect, 4, 4, shadowPaint);

        // Draw background
        using var bgPaint = new SKPaint
        {
            Color = SKColors.White,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        canvas.DrawRoundRect(rect, 4, 4, bgPaint);

        // Draw border
        using var borderPaint = new SKPaint
        {
            Color = borderColor,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true
        };
        canvas.DrawRoundRect(rect, 4, 4, borderPaint);

        // Draw service indicator bar
        using var barPaint = new SKPaint
        {
            Color = borderColor,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        var barRect = new SKRect(x, y, x + 4, y + height);
        canvas.DrawRoundRect(barRect, 4, 0, barPaint);

        // Draw label
        using var labelFont = new SKFont(SKTypeface.FromFamilyName("Arial"), 12);
        using var labelPaint = new SKPaint
        {
            Color = SKColor.Parse("#333333"),
            IsAntialias = true
        };

        var label = TruncateLabel(node.Data.Label, 28);
        var labelY = y + height / 2 - 6;
        canvas.DrawText(label, x + 14, labelY + 12, SKTextAlign.Left, labelFont, labelPaint);

        // Draw resource type
        using var typeFont = new SKFont(SKTypeface.FromFamilyName("Arial"), 10);
        using var typePaint = new SKPaint
        {
            Color = SKColor.Parse("#666666"),
            IsAntialias = true
        };

        var resourceType = node.Data.ResourceType ?? "resource";
        var typeY = y + height / 2 + 10;
        canvas.DrawText(resourceType, x + 14, typeY + 4, SKTextAlign.Left, typeFont, typePaint);
    }

    private static void DrawEdge(SKCanvas canvas, ReactFlowEdge edge, List<ReactFlowNode> nodes, ExportOptions options)
    {
        var sourceNode = nodes.FirstOrDefault(n => n.Id == edge.Source);
        var targetNode = nodes.FirstOrDefault(n => n.Id == edge.Target);

        if (sourceNode == null || targetNode == null)
            return;

        // Calculate edge points
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

        var edgeColor = GetEdgeColor(edge.Data?.RelationshipType);
        var strokeWidth = edge.Data?.RelationshipType?.ToLowerInvariant() switch
        {
            "belongsto" or "references" => 1,
            _ => 2
        };

        using var path = new SKPath();

        // Create smoothstep path
        var midX = (adjustedSourceX + adjustedTargetX) / 2;
        var midY = (adjustedSourceY + adjustedTargetY) / 2;
        var dx = Math.Abs(adjustedTargetX - adjustedSourceX);
        var dy = Math.Abs(adjustedTargetY - adjustedSourceY);

        path.MoveTo((float)adjustedSourceX, (float)adjustedSourceY);

        if (dx > dy)
        {
            path.CubicTo(
                (float)midX, (float)adjustedSourceY,
                (float)midX, (float)adjustedTargetY,
                (float)adjustedTargetX, (float)adjustedTargetY);
        }
        else
        {
            path.CubicTo(
                (float)adjustedSourceX, (float)midY,
                (float)adjustedTargetX, (float)midY,
                (float)adjustedTargetX, (float)adjustedTargetY);
        }

        using var paint = new SKPaint
        {
            Color = edgeColor,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = strokeWidth,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round
        };

        // Apply dash pattern for certain relationship types
        var relType = edge.Data?.RelationshipType?.ToLowerInvariant();
        if (relType == "belongsto")
        {
            paint.PathEffect = SKPathEffect.CreateDash([4, 2], 0);
        }
        else if (relType == "references")
        {
            paint.PathEffect = SKPathEffect.CreateDash([2, 2], 0);
        }

        canvas.DrawPath(path, paint);

        // Draw arrow
        DrawArrow(canvas, adjustedSourceX, adjustedSourceY, adjustedTargetX, adjustedTargetY, edgeColor);

        // Draw label
        if (options.IncludeEdgeLabels && !string.IsNullOrEmpty(edge.Label))
        {
            DrawEdgeLabel(canvas, edge.Label, adjustedSourceX, adjustedSourceY, adjustedTargetX, adjustedTargetY);
        }
    }

    private static void DrawArrow(SKCanvas canvas, double fromX, double fromY, double toX, double toY, SKColor color)
    {
        var angle = Math.Atan2(toY - fromY, toX - fromX);
        var arrowLength = 8;
        var arrowAngle = Math.PI / 6; // 30 degrees

        var x1 = toX - arrowLength * Math.Cos(angle - arrowAngle);
        var y1 = toY - arrowLength * Math.Sin(angle - arrowAngle);
        var x2 = toX - arrowLength * Math.Cos(angle + arrowAngle);
        var y2 = toY - arrowLength * Math.Sin(angle + arrowAngle);

        using var path = new SKPath();
        path.MoveTo((float)toX, (float)toY);
        path.LineTo((float)x1, (float)y1);
        path.LineTo((float)x2, (float)y2);
        path.Close();

        using var paint = new SKPaint
        {
            Color = color,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        canvas.DrawPath(path, paint);
    }

    private static void DrawEdgeLabel(SKCanvas canvas, string label, double x1, double y1, double x2, double y2)
    {
        var midX = (float)((x1 + x2) / 2);
        var midY = (float)((y1 + y2) / 2);

        // Draw background
        using var bgPaint = new SKPaint
        {
            Color = SKColors.White,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        var bgRect = new SKRect(midX - 30, midY - 8, midX + 30, midY + 8);
        canvas.DrawRoundRect(bgRect, 3, 3, bgPaint);

        // Draw text
        using var font = new SKFont(SKTypeface.FromFamilyName("Arial"), 10);
        using var textPaint = new SKPaint
        {
            Color = SKColor.Parse("#666666"),
            IsAntialias = true
        };

        canvas.DrawText(label, midX, midY + 4, SKTextAlign.Center, font, textPaint);
    }

    private static (double x, double y) AdjustPointToNodeEdge(
        double pointX, double pointY, double otherX, double otherY,
        double nodeX, double nodeY, double nodeWidth, double nodeHeight)
    {
        var dx = otherX - pointX;
        var dy = otherY - pointY;
        var distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance == 0) return (pointX, pointY);

        dx /= distance;
        dy /= distance;

        var halfWidth = nodeWidth / 2;
        var halfHeight = nodeHeight / 2;

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

    private static SKColor GetServiceColor(string? service)
    {
        if (service == null) return SKColor.Parse("#666666");
        return ServiceColors.GetValueOrDefault(service, SKColor.Parse("#666666"));
    }

    private static SKColor GetEdgeColor(string? relationshipType)
    {
        if (relationshipType == null) return SKColor.Parse("#666666");
        return EdgeColors.GetValueOrDefault(relationshipType.ToLowerInvariant(), SKColor.Parse("#666666"));
    }

    private static GroupNodeStyle GetGroupStyle(string? groupType)
    {
        return groupType switch
        {
            "vpc" => new GroupNodeStyle(
                SKColor.Parse("#FF9900").WithAlpha(12),
                SKColor.Parse("#FF9900"),
                2, 8, true, [8, 4]),
            "subnet" => new GroupNodeStyle(
                SKColor.Parse("#FF9900").WithAlpha(5),
                SKColor.Parse("#FF9900"),
                1, 4, true, [4, 2]),
            "ecs-cluster" => new GroupNodeStyle(
                SKColor.Parse("#FF9900").WithAlpha(20),
                SKColor.Parse("#FF9900"),
                2, 8, false, []),
            "load-balancer" => new GroupNodeStyle(
                SKColor.Parse("#8C4FFF").WithAlpha(12),
                SKColor.Parse("#8C4FFF"),
                2, 8, false, []),
            _ => new GroupNodeStyle(
                SKColor.Parse("#888888").WithAlpha(12),
                SKColor.Parse("#888888"),
                1, 4, true, [4, 2])
        };
    }

    private static SKColor ParseColor(string color)
    {
        try
        {
            return SKColor.Parse(color);
        }
        catch
        {
            return SKColors.White;
        }
    }

    private static string TruncateLabel(string label, int maxLength)
    {
        if (label.Length <= maxLength) return label;
        return label[..(maxLength - 3)] + "...";
    }

    private sealed record DiagramBounds(double MinX, double MinY, double Width, double Height)
    {
        public double MaxX => MinX + Width;
        public double MaxY => MinY + Height;
    }

    private sealed record GroupNodeStyle(
        SKColor BackgroundColor,
        SKColor BorderColor,
        int BorderWidth,
        int BorderRadius,
        bool IsDashed,
        float[] DashPattern);
}
