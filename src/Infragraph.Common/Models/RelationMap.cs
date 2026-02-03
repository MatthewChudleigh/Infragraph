using Infragraph.Common.Models.Domain;
using Infragraph.Common.Models.Graph;

namespace Infragraph.Core.Graph;

public class RelationMap(ICollection<GraphNode> nodes, ICollection<GraphEdge> edges)
{
    public ICollection<GraphNode> Nodes => nodes;
    public ICollection<GraphEdge> Edges => edges;
    
    public readonly Dictionary<string, string> ContainedBy = new();
    public readonly Dictionary<string, string> BelongsTo = new();
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
            switch (edge.RelationshipType)
            {
                case RelationshipType.Contains:
                    Add(map.Contains, edge.Source, edge.Target);
                    map.ContainedBy[edge.Target] = edge.Source; 
                    break;
                case RelationshipType.BelongsTo:
                    map.BelongsTo[edge.Source] = edge.Target;
                    break;
                case RelationshipType.Uses:
                    Add(map.Uses, edge.Source, edge.Target);
                    // Build reverse lookup: target -> list of sources that use it
                    Add(map.UsedBy, edge.Target, edge.Source);
                    break;
                case RelationshipType.AttachedTo:
                    Add(map.AttachedTo, edge.Source, edge.Target);
                    break;
                case RelationshipType.Assumes:
                    Add(map.AssumedBy, edge.Target, edge.Source);
                    break;
            }
        }
        
        return map;

        void Add(Dictionary<string, List<string>> dict, string key, string value)
        {
            if (!dict.ContainsKey(key))
                dict[key] = [];
            dict[key].Add(value);
        }
    }
}