// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Models;

public class ContestSummaryEntryDetails
{
    public DomainOfInfluenceType DomainOfInfluenceType { get; set; }

    public int ContestEntriesCount { get; set; }
}
