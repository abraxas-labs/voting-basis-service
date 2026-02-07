// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;

namespace Voting.Basis.Data.Models;

public class Vote : PoliticalBusiness
{
    private PoliticalBusinessSubType? _politicalBusinessSubType;

    public ICollection<Ballot> Ballots { get; set; } = new HashSet<Ballot>();

    public int ReportDomainOfInfluenceLevel { get; set; }

    public override PoliticalBusinessType PoliticalBusinessType => PoliticalBusinessType.Vote;

    // The sub type is only accurate if the ballots have been loaded (or no ballots exist).
    // Otherwise, it should be calculated manually
    public override PoliticalBusinessSubType PoliticalBusinessSubType
        => _politicalBusinessSubType
            ?? CalculateSubType(Ballots.Any(b => b.BallotType == BallotType.VariantsBallot));

    public VoteResultAlgorithm ResultAlgorithm { get; set; }

    public int BallotBundleSampleSizePercent { get; set; }

    public bool AutomaticBallotBundleNumberGeneration { get; set; }

    public bool AutomaticBallotNumberGeneration { get; set; }

    public bool EnforceResultEntryForCountingCircles { get; set; }

    public VoteResultEntry ResultEntry { get; set; }

    public VoteReviewProcedure ReviewProcedure { get; set; }

    public bool EnforceReviewProcedureForCountingCircles { get; set; }

    public string InternalDescription { get; set; } = string.Empty;

    public VoteType Type { get; set; }

    public void UpdateSubTypeManually(bool hasBallotWithVariantBallotType)
    {
        _politicalBusinessSubType = CalculateSubType(hasBallotWithVariantBallotType);
    }

    private PoliticalBusinessSubType CalculateSubType(bool hasBallotWithVariantBallotType)
    {
        return Type == VoteType.VariantQuestionsOnMultipleBallots || hasBallotWithVariantBallotType
            ? PoliticalBusinessSubType.VoteVariantBallot
            : PoliticalBusinessSubType.Unspecified;
    }
}
