// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Basis.Data.Models;

public abstract class MajorityElectionCandidateBase : ElectionCandidate
{
    public Dictionary<string, string> PartyShortDescription { get; set; } = new();

    public Dictionary<string, string> PartyLongDescription { get; set; } = new();

    public MajorityElectionCandidateReportingType ReportingType { get; set; }
}
