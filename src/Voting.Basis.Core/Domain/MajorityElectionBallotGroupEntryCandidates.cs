// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Basis.Core.Domain;

public class MajorityElectionBallotGroupEntryCandidates
{
    public Guid BallotGroupEntryId { get; set; }

    public List<Guid> CandidateIds { get; set; } = new();

    public int IndividualCandidatesVoteCount { get; set; }
}
