// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public class MajorityElectionBallotGroupEntry : BaseEntity
{
    public int BlankRowCount { get; set; }

    public Guid BallotGroupId { get; set; }

    public MajorityElectionBallotGroup BallotGroup { get; set; } = null!;

    public Guid? PrimaryMajorityElectionId { get; set; }

    public MajorityElection? PrimaryMajorityElection { get; set; }

    public Guid? SecondaryMajorityElectionId { get; set; }

    public SecondaryMajorityElection? SecondaryMajorityElection { get; set; }

    public ICollection<MajorityElectionBallotGroupEntryCandidate> Candidates { get; set; } = new HashSet<MajorityElectionBallotGroupEntryCandidate>();

    public int IndividualCandidatesVoteCount { get; set; }

    public int CountOfCandidates { get; set; }

    public bool CandidateCountOk { get; set; }

    /// <summary>
    /// Checks whether the number of mandates equals the sum of <see cref="BlankRowCount"/> + <see cref="IndividualCandidatesVoteCount"/> + <see cref="CountOfCandidates"/>.
    /// </summary>
    /// <param name="numberOfMandates">The number of mandates in the election. If not provided, the value is resolved from the <see cref="PrimaryMajorityElection"/> or <see cref="SecondaryMajorityElection"/>.</param>
    /// <exception cref="InvalidOperationException">Thrown if the number of mandates could not be resolved.</exception>
    public void UpdateCandidateCountOk(int? numberOfMandates = null)
    {
        numberOfMandates ??= PrimaryMajorityElection?.NumberOfMandates
                             ?? SecondaryMajorityElection?.NumberOfMandates
                             ?? throw new InvalidOperationException("could not resolve number of mandates");
        CandidateCountOk = numberOfMandates == BlankRowCount + IndividualCandidatesVoteCount + CountOfCandidates;
    }
}
