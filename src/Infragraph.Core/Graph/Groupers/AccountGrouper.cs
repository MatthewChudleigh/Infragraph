using Infragraph.Common.Abstractions;
using Infragraph.Common.Models.Graph;

namespace Infragraph.Core.Graph.Groupers;

/// <summary>
/// Groups nodes by AWS account. Creates top-level account groups that contain
/// all other groups and ungrouped resources.
/// </summary>
public sealed class AccountGrouper : IGroupingStrategy
{
    public string GroupingType => IGroupingStrategy.GroupType.Account;
    public int Priority => 0; // Applied first - creates top-level account groups

    /// <summary>
    /// Gets the account group ID for a given account.
    /// </summary>
    public static string GetAccountGroupId(string account) => $"group-account-{account}";

    public IEnumerable<NodeGroup> GroupNodes(RelationMap map)
    {
        // Get all unique accounts from nodes
        var accounts = map.Nodes
            .Select(n => n.Data.TryGetValue(IGroupingStrategy.GroupType.Account, out var acc) ? acc?.ToString() : null)
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Distinct()
            .OrderBy(a => a)
            .ToList();

        // Create an account group for each account
        foreach (var account in accounts)
        {
            if (account == null) continue;

            yield return new NodeGroup
            {
                Id = GetAccountGroupId(account),
                Label = account,
                GroupType = IGroupingStrategy.GroupType.Account,
                ParentId = null, // Top-level group
                NodeIds = [], // Nodes will be assigned by GraphBuilder after other groupers run
                Data = new Dictionary<string, object>
                {
                    ["account"] = account
                }
            };
        }
    }
}
