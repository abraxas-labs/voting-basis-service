// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Basis.Core.Domain;

public class MajorityElectionBallotGroupEntry
{
    public Guid Id { get; internal set; }

    public Guid ElectionId { get; private set; }

    /// <summary>
    /// Gets the count of blank/empty rows.
    /// </summary>
    public int BlankRowCount { get; private set; }

    public List<string> CandidateIds { get; private set; } = new();

    /// <summary>
    /// Gets the count of "individual candidates".
    /// </summary>
    public int IndividualCandidatesVoteCount { get; internal set; }

    public bool CandidateCountOk(int numberOfMandates)
        => numberOfMandates == BlankRowCount + CandidateIds.Count + IndividualCandidatesVoteCount;
}
