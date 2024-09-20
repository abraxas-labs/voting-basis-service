// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public class ProportionalElectionUnionList : BaseEntity
{
    // for ef
    public ProportionalElectionUnionList()
    {
    }

    public ProportionalElectionUnionList(
        Guid unionId,
        string orderNumber,
        Dictionary<string, string> shortDescription,
        List<ProportionalElectionList> lists)
    {
        OrderNumber = orderNumber;
        ShortDescription = shortDescription;
        ProportionalElectionUnionId = unionId;
        ProportionalElectionUnionListEntries = lists.ConvertAll(l => new ProportionalElectionUnionListEntry
        {
            ProportionalElectionListId = l.Id,
        });
    }

    // copied from matching lists
    public string OrderNumber { get; set; } = string.Empty;

    // copied from matching lists
    public Dictionary<string, string> ShortDescription { get; set; } = new Dictionary<string, string>();

    public Guid ProportionalElectionUnionId { get; set; }

    public ProportionalElectionUnion ProportionalElectionUnion { get; set; } = null!; // set by ef

    public ICollection<ProportionalElectionUnionListEntry> ProportionalElectionUnionListEntries { get; set; }
        = new HashSet<ProportionalElectionUnionListEntry>();

    [NotMapped]
    public int ListCount => ProportionalElectionUnionListEntries.Count;

    [NotMapped]
    public string PoliticalBusinessNumbers => string.Join(" ", ProportionalElectionUnionListEntries
        .Select(e => e.ProportionalElectionList?.ProportionalElection?.PoliticalBusinessNumber ?? string.Empty)
        .OrderBy(n => n));
}
