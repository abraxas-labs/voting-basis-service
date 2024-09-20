// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Basis.Data.Models;
using MajorityElection = Voting.Basis.Core.Domain.MajorityElection;

namespace Voting.Basis.Core.Validation;

public class MajorityElectionValidator : AbstractValidator<MajorityElection>
{
    public MajorityElectionValidator()
    {
        RuleFor(m => m.BallotBundleSampleSize).LessThanOrEqualTo(m => m.BallotBundleSize);
        RuleFor(m => m.BallotNumberGeneration).Equal(BallotNumberGeneration.RestartForEachBundle)
            .Unless(m => m.AutomaticBallotBundleNumberGeneration);
    }
}
