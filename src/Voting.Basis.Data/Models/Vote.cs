// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Basis.Data.Models;

public class Vote : PoliticalBusiness
{
    public ICollection<Ballot> Ballots { get; set; } = new HashSet<Ballot>();

    public int ReportDomainOfInfluenceLevel { get; set; }

    public override PoliticalBusinessType PoliticalBusinessType => PoliticalBusinessType.Vote;

    public VoteResultAlgorithm ResultAlgorithm { get; set; }

    public int BallotBundleSampleSizePercent { get; set; }

    public bool AutomaticBallotBundleNumberGeneration { get; set; }

    public bool EnforceResultEntryForCountingCircles { get; set; }

    public VoteResultEntry ResultEntry { get; set; }

    public VoteReviewProcedure ReviewProcedure { get; set; }

    public bool EnforceReviewProcedureForCountingCircles { get; set; }

    public string InternalDescription { get; set; } = string.Empty;
}
