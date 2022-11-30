// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Basis.Data.Models;

public class SecondaryMajorityElection : PoliticalBusiness
{
    public int NumberOfMandates { get; set; }

    public SecondaryMajorityElectionAllowedCandidate AllowedCandidates { get; set; }

    public Guid PrimaryMajorityElectionId { get; set; }

    public MajorityElection PrimaryMajorityElection { get; set; } = null!; // set by EF

    public override PoliticalBusinessType PoliticalBusinessType => PoliticalBusinessType.SecondaryMajorityElection;

    public Guid ElectionGroupId { get; set; }

    public ElectionGroup ElectionGroup { get; set; } = null!; // set by EF

    public ICollection<SecondaryMajorityElectionCandidate> Candidates { get; set; } = new HashSet<SecondaryMajorityElectionCandidate>();

    public ICollection<MajorityElectionBallotGroupEntry> BallotGroupEntries { get; set; } = new HashSet<MajorityElectionBallotGroupEntry>();

    public override Guid DomainOfInfluenceId
    {
        get => PrimaryMajorityElection.DomainOfInfluenceId;
        set => throw new InvalidOperationException($"{nameof(DomainOfInfluenceId)} is read only.");
    }

    public override DomainOfInfluence? DomainOfInfluence
    {
        get => PrimaryMajorityElection.DomainOfInfluence;
        set => throw new InvalidOperationException($"{nameof(DomainOfInfluence)} is read only.");
    }

    public override Guid ContestId
    {
        get => PrimaryMajorityElection.ContestId;
        set => throw new InvalidOperationException($"{nameof(ContestId)} is read only.");
    }

    public override Contest Contest
    {
        get => PrimaryMajorityElection.Contest;
        set => throw new InvalidOperationException($"{nameof(Contest)} is read only.");
    }
}
