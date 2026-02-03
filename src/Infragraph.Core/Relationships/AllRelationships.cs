using Infragraph.Common.Abstractions;

namespace Infragraph.Core.Relationships;

public class AllRelationships
{
    public static ICollection<IRelationshipExtractor> All()
    {
        return
        [
            new ComputeRelationship(),
            new EcsRelationship(),
            new ElbRelationship(),
            new IamRelationship(),
            new NetworkRelationship(),
            new SecurityRelationship()
        ];
    }
}