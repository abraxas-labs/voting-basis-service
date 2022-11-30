// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Models;

public class ContestSummary
{
    public Contest Contest { get; set; } = null!; // loaded by ef

    public List<ContestSummaryEntryDetails>? ContestEntriesDetails { get; set; }
}
