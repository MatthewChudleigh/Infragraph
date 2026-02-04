using Infragraph.Common.Abstractions;
using Infragraph.Common.Configuration;
using Infragraph.Common.Models.Graph;

namespace Infragraph.Core.Graph.Groupers;

public class NetworkGrouper : IGroupingStrategy
{
    public string GroupingType => IGroupingStrategy.GroupType.Network;
    public int Priority => 6;
    
    public IEnumerable<NodeGroup> GroupNodes(RelationMap map)
    {
        // Get all unique accounts from nodes
        var networkGroups = map.Nodes
            .Where(n => n.Data.ContainsKey("account")
                        && n.ResourceType is SupportedResourceTypes.RouteTable
                            or SupportedResourceTypes.Route
                            or SupportedResourceTypes.InternetGateway
                            or SupportedResourceTypes.NatGateway
            )
            .GroupBy(n => (string)n.Data["account"]);
            
        // Create an account group for each account
        foreach (var networkGroup in networkGroups)
        {
            yield return new NodeGroup
            {
                Id = $"{IGroupingStrategy.GroupType.Network}-{networkGroup.Key}",
                Label = $"{IGroupingStrategy.GroupType.Network}-{networkGroup.Key}",
                GroupType = IGroupingStrategy.GroupType.Network,
                ParentId = AccountGrouper.GetAccountGroupId(networkGroup.Key),
                NodeIds = networkGroup.Select(n => n.Id).ToList(), // Nodes will be assigned by GraphBuilder after other groupers run
                Data = new Dictionary<string, object>
                {
                    ["account"] = networkGroup.Key
                }
            };
        }
    }
    
}