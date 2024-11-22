// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Text.Encodings.Web;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Utils;

internal static class ProportionalListUnionDescriptionBuilder
{
    private const int MaxListUnionDescriptions = 7;

    /// <summary>
    /// Builds a description of all lists which are connected to the provided list via a (sub-)list-union.
    /// HTML is used to mark the main list. User entered values are escaped, so the HTML is safe to display.
    /// This description is built here instead of a frontend since it requires lots of different data.
    /// </summary>
    /// <param name="list">The list, including list unions, its entries and the lists of the entries.</param>
    /// <param name="subListUnion">Whether the description should be built for sub list unions or default list unions.</param>
    /// <returns>The built description.</returns>
    internal static string BuildListUnionDescription(ProportionalElectionList list, bool subListUnion)
    {
        var lists = list.ProportionalElectionListUnionEntries
            .Select(x => x.ProportionalElectionListUnion)
            .Where(x => x.IsSubListUnion == subListUnion)
            .SelectMany(l => l.ProportionalElectionListUnionEntries
                .Select(le => new
                {
                    le.ProportionalElectionList.Id,
                    le.ProportionalElectionList.Position,
                    MainList = l.ProportionalElectionMainListId == le.ProportionalElectionListId,
                    OrderNumber = HtmlEncoder.Default.Encode(le.ProportionalElectionList.OrderNumber),
                })
                .OrderByDescending(x => x.MainList)
                .ThenBy(x => x.Position)

                // distinct by after order by to ensure the mainlist is included.
                .DistinctBy(x => x.Id))
            .ToList();

        var relevantListsInOrder = lists
            .Take(MaxListUnionDescriptions)
            .OrderBy(x => x.Position)
            .ToList();

        var description = relevantListsInOrder
            .ConvertAll(le => $"<span{(le.MainList ? " class=\"main-list\"" : string.Empty)}>{le.OrderNumber}</span>");

        if (lists.Count > MaxListUnionDescriptions)
        {
            description.Add("<span>…</span>");
        }

        return $"<span>{string.Join(", ", description)}</span>";
    }
}
