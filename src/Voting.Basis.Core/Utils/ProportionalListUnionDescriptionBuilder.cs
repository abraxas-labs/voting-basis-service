// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using Voting.Basis.Data.Models;
using Voting.Lib.Common;

namespace Voting.Basis.Core.Utils;

internal static class ProportionalListUnionDescriptionBuilder
{
    private const int MaxListUnionDescriptions = 3;

    /// <summary>
    /// Builds a description of all lists which are connected to the provided list via a (sub-)list-union.
    /// HTML is used to mark the main list. User entered values are escaped, so the HTML is safe to display.
    /// This description is built here instead of a frontend since it requires lots of different data.
    /// </summary>
    /// <param name="list">The list, including list unions, its entries and the lists of the entries.</param>
    /// <param name="subListUnion">Whether the description should be built for sub list unions or default list unions.</param>
    /// <returns>The built description for each language.</returns>
    internal static Dictionary<string, string> BuildListUnionDescription(ProportionalElectionList list, bool subListUnion)
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
                    Text = le.ProportionalElectionList.ShortDescription.ToDictionary(x => x.Key, x => HtmlEncoder.Default.Encode(x.Value)),
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

        var descriptions = new Dictionary<string, string>();
        foreach (var lang in Languages.All)
        {
            var description = relevantListsInOrder
                .ConvertAll(le => $"<span{(le.MainList ? " class=\"main-list\"" : string.Empty)}>{le.Text.GetValueOrDefault(lang, string.Empty)}</span>");

            if (lists.Count > MaxListUnionDescriptions)
            {
                description.Add("<span>…</span>");
            }

            descriptions.Add(lang, $"<span>{string.Join(", ", description)}</span>");
        }

        return descriptions;
    }
}
