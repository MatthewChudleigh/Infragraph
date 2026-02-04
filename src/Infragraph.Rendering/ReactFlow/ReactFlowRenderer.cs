namespace Infragraph.Rendering.ReactFlow;

using Common.Abstractions;
using Common.Configuration;
using Common.Models.Domain;
using Common.Models.Graph;
using Infragraph.Common.Models.ReactFlow;

/// <summary>
/// Renders InfraGraph to React Flow format.
/// </summary>
public sealed class ReactFlowRenderer : IRenderer<ReactFlowDiagram>
{
    private static readonly Dictionary<string, string> ServiceColors = new()
    {
        [SupportedServiceTypes.Ec2] = "#FF9900",
        [SupportedServiceTypes.Ecs] = "#FF9900",
        [SupportedServiceTypes.ElbV2] = "#8C4FFF",
        [SupportedServiceTypes.Iam] = "#DD4B39",
        [SupportedServiceTypes.S3] = "#3F8624",
        [SupportedServiceTypes.Rds] = "#3B48CC",
        [SupportedServiceTypes.DynamoDb] = "#3B48CC",
        [SupportedServiceTypes.Lambda] = "#FF9900",
        [SupportedServiceTypes.Sqs] = "#FF4F8B",
        [SupportedServiceTypes.Sns] = "#FF4F8B",
        [SupportedServiceTypes.CloudWatchLogs] = "#FF4F8B",
        [SupportedServiceTypes.SecretsManager] = "#DD4B39"
    };

    public ReactFlowDiagram Render(InfraGraph graph, DiagramOptions options)
    {
        // Create group nodes first (they need to be in the nodes array for React Flow)
        var nodes = graph.Groups
            .OrderBy(g => g.GroupType == IGroupingStrategy.GroupType.Account ? 0 : 1)
            .Select(CreateGroupNode)
            .ToList();
        // Create resource nodes
        nodes.AddRange(graph.Nodes.Select(node => CreateResourceNode(node, options)));

        // Create edges
        var edges = (
            from edge in graph.Edges 
            where edge.RelationshipType != RelationshipType.Contains 
            select CreateEdge(edge)).ToList();

        // Build metadata
        var metadata = new ReactFlowMetadata
        {
            TotalResources = graph.Metadata.TotalResources,
            IncludedResources = graph.Metadata.IncludedResources,
            TotalRelationships = graph.Metadata.TotalRelationships,
            ResourceTypes = graph.Metadata.ResourceTypes,
            Regions = graph.Metadata.Regions,
            GeneratedAt = graph.Metadata.GeneratedAt,
            ElkOptions = new ElkLayoutOptions
            {
                Algorithm = "layered",
                Direction = GetElkDirection(options.LayoutDirection),
                NodeSpacing = 50,
                LayerSpacing = 100
            }
        };

        return new ReactFlowDiagram
        {
            Nodes = nodes,
            Edges = edges,
            Metadata = metadata
        };
    }

    private static ReactFlowNode CreateGroupNode(NodeGroup group)
    {
        return new ReactFlowNode
        {
            Id = group.Id,
            Type = "awsGroup",
            Position = new ReactFlowPosition { X = 0, Y = 0 },
            Data = new ReactFlowNodeData
            {
                Label = group.Label,
                GroupType = group.GroupType,
                IsGroup = true,
                Extra = group.Data.Count > 0 ? new Dictionary<string, object>(group.Data) : null
            },
            ParentId = group.ParentId,
            Style = GetGroupStyle(group.GroupType)
        };
    }

    private static ReactFlowNode CreateResourceNode(GraphNode node, DiagramOptions options)
    {
        var color = GetServiceColor(node.Service);

        return new ReactFlowNode
        {
            Id = node.Id,
            Type = "awsResource",
            Position = new ReactFlowPosition { X = 0, Y = 0 },
            Data = new ReactFlowNodeData
            {
                Label = node.Label,
                ResourceType = node.ResourceType,
                Service = node.Service,
                Arn = node.Data.GetValueOrDefault("arn")?.ToString(),
                Extra = node.Data.Count > 0 ? new Dictionary<string, object>(node.Data) : null
            },
            ParentId = node.ParentId,
            Extent = node.ParentId != null ? "parent" : null,
            Width = options.DefaultNodeWidth,
            Height = options.DefaultNodeHeight,
            Style = new Dictionary<string, object>
            {
                ["borderColor"] = color,
                ["backgroundColor"] = "#ffffff"
            }
        };
    }

    private static ReactFlowEdge CreateEdge(GraphEdge edge)
    {
        var edgeStyle = GetEdgeStyle(edge.RelationshipType);

        return new ReactFlowEdge
        {
            Id = edge.Id,
            Source = edge.Source,
            Target = edge.Target,
            Type = "smoothstep",
            Label = edge.Label,
            Animated = edge.RelationshipType == RelationshipType.Uses,
            Style = edgeStyle,
            Data = new ReactFlowEdgeData
            {
                RelationshipType = edge.RelationshipType.ToString()
            },
            MarkerEnd = new ReactFlowMarker
            {
                Type = "arrowclosed",
                Color = edgeStyle.GetValueOrDefault("stroke")?.ToString() ?? "#888"
            }
        };
    }

    private static string GetServiceColor(string service)
    {
        return ServiceColors.GetValueOrDefault(service, "#666666");
    }

    private static Dictionary<string, object> GetGroupStyle(string groupType)
    {
        return groupType switch
        {
            "vpc" => new Dictionary<string, object>
            {
                ["backgroundColor"] = "rgba(255, 153, 0, 0.05)",
                ["borderColor"] = "#FF9900",
                ["borderWidth"] = 2,
                ["borderStyle"] = "dashed",
                ["borderRadius"] = 8,
                ["padding"] = 20
            },
            "subnet" => new Dictionary<string, object>
            {
                ["backgroundColor"] = "rgba(255, 153, 0, 0.02)",
                ["borderColor"] = "#FF9900",
                ["borderWidth"] = 1,
                ["borderStyle"] = "dotted",
                ["borderRadius"] = 4,
                ["padding"] = 16
            },
            "ecs-cluster" => new Dictionary<string, object>
            {
                ["backgroundColor"] = "rgba(255, 153, 0, 0.08)",
                ["borderColor"] = "#FF9900",
                ["borderWidth"] = 2,
                ["borderStyle"] = "solid",
                ["borderRadius"] = 8,
                ["padding"] = 16
            },
            "load-balancer" => new Dictionary<string, object>
            {
                ["backgroundColor"] = "rgba(140, 79, 255, 0.05)",
                ["borderColor"] = "#8C4FFF",
                ["borderWidth"] = 2,
                ["borderStyle"] = "solid",
                ["borderRadius"] = 8,
                ["padding"] = 16
            },
            _ => new Dictionary<string, object>
            {
                ["backgroundColor"] = "rgba(128, 128, 128, 0.05)",
                ["borderColor"] = "#888888",
                ["borderWidth"] = 1,
                ["borderStyle"] = "dashed",
                ["borderRadius"] = 4,
                ["padding"] = 12
            }
        };
    }

    private static Dictionary<string, object> GetEdgeStyle(RelationshipType type)
    {
        return type switch
        {
            RelationshipType.BelongsTo => new Dictionary<string, object>
            {
                ["stroke"] = "#888888",
                ["strokeWidth"] = 1,
                ["strokeDasharray"] = "4 2"
            },
            RelationshipType.Uses => new Dictionary<string, object>
            {
                ["stroke"] = "#4CAF50",
                ["strokeWidth"] = 2
            },
            RelationshipType.AttachedTo => new Dictionary<string, object>
            {
                ["stroke"] = "#2196F3",
                ["strokeWidth"] = 2
            },
            RelationshipType.References => new Dictionary<string, object>
            {
                ["stroke"] = "#FF5722",
                ["strokeWidth"] = 1,
                ["strokeDasharray"] = "2 2"
            },
            RelationshipType.Assumes => new Dictionary<string, object>
            {
                ["stroke"] = "#9C27B0",
                ["strokeWidth"] = 2
            },
            RelationshipType.RoutesTo => new Dictionary<string, object>
            {
                ["stroke"] = "#FF9800",
                ["strokeWidth"] = 2
            },
            RelationshipType.ListensFor => new Dictionary<string, object>
            {
                ["stroke"] = "#8C4FFF",
                ["strokeWidth"] = 2
            },
            RelationshipType.Targets => new Dictionary<string, object>
            {
                ["stroke"] = "#00BCD4",
                ["strokeWidth"] = 2
            },
            _ => new Dictionary<string, object>
            {
                ["stroke"] = "#666666",
                ["strokeWidth"] = 1
            }
        };
    }

    private static string GetElkDirection(LayoutDirection direction)
    {
        return direction switch
        {
            LayoutDirection.TopToBottom => "DOWN",
            LayoutDirection.LeftToRight => "RIGHT",
            LayoutDirection.BottomToTop => "UP",
            LayoutDirection.RightToLeft => "LEFT",
            _ => "DOWN"
        };
    }
}
