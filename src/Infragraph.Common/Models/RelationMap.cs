using Infragraph.Common.Models.Domain;
using Infragraph.Common.Models.Graph;

namespace Infragraph.Core.Graph;

public class RelationMap(ICollection<GraphNode> nodes, ICollection<GraphEdge> edges)
{
    public ICollection<GraphNode> Nodes => nodes;
    public ICollection<GraphEdge> Edges => edges;
    
    public readonly Dictionary<string, string> ContainedBy = new();
    public readonly Dictionary<string, List<string>> Contains = new();
    public readonly Dictionary<string, List<string>> Uses = new();
    public readonly Dictionary<string, List<string>> UsedBy = new();
    public readonly Dictionary<string, List<string>> AssumedBy = new();
    public readonly Dictionary<string, List<string>> AttachedTo = new();
    
    public static RelationMap Map(ICollection<GraphNode> nodes, ICollection<GraphEdge> edges)
    {
        var map = new RelationMap(nodes, edges);
        
        // Build containment map (parent -> children)
        foreach (var edge in edges.Where(e => e.RelationshipType == RelationshipType.Contains))
        {
            if (!map.Contains.ContainsKey(edge.Source))
                map.Contains[edge.Source] = [];
            map.Contains[edge.Source].Add(edge.Target);
            map.ContainedBy[edge.Target] = edge.Source;
        }
        
        foreach (var edge in edges.Where(e => e.RelationshipType == RelationshipType.Uses))
        {
            if (!map.Uses.ContainsKey(edge.Source))
                map.Uses[edge.Source] = [];
            map.Uses[edge.Source].Add(edge.Target); 
            
            // Build reverse lookup: target -> list of sources that use it
            if (!map.UsedBy.ContainsKey(edge.Target))
                map.UsedBy[edge.Target] = [];
            map.UsedBy[edge.Target].Add(edge.Source);
        }

        // Build forward lookup for AttachedTo: source -> list of targets
        foreach (var edge in edges.Where(e => e.RelationshipType == RelationshipType.AttachedTo))
        {
            if (!map.AttachedTo.ContainsKey(edge.Source))
                map.AttachedTo[edge.Source] = [];
            map.AttachedTo[edge.Source].Add(edge.Target);
        }
        
        // Build assumes map to find role consumers (who assumes the role)
        foreach (var edge in edges.Where(e => e.RelationshipType == RelationshipType.Assumes))
        {
            if (!map.AssumedBy.ContainsKey(edge.Target))
                map.AssumedBy[edge.Target] = [];
            map.AssumedBy[edge.Target].Add(edge.Source);
        }
        return map;
    }
}