// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using ProportionalElection = Voting.Basis.Core.Domain.ProportionalElection;

namespace Voting.Basis.Core.Validation;

public class ProportionalElectionValidator : AbstractValidator<ProportionalElection>
{
    public ProportionalElectionValidator()
    {
        RuleFor(m => m.BallotBundleSampleSize).LessThanOrEqualTo(m => m.BallotBundleSize);
        RuleFor(p => p.MandateAlgorithm)
            .IsInEnum(); // prevent deprecated proto mandate algorithms inputs.
    }
}
