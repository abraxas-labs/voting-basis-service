// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using FluentValidation;
using Voting.Basis.Core.Domain;

namespace Voting.Basis.Core.Validation;

public class CantonSettingsValidator : AbstractValidator<CantonSettings>
{
    public CantonSettingsValidator(CantonSettingsVotingCardChannelValidator votingCardChannelValidator, CountingCircleResultStateDescriptionValidator countingCircleResultStateDescriptionValidator)
    {
        RuleFor(v => v.ProportionalElectionMandateAlgorithms).NotEmpty();
        RuleFor(x => x.EnabledVotingCardChannels).Must(x =>
            x is { Count: > 0 }
            && x.DistinctBy(y => (y.Valid, y.VotingChannel)).Count() == x.Count);
        RuleForEach(v => v.EnabledVotingCardChannels)
            .SetValidator(votingCardChannelValidator);
        RuleFor(x => x.CountingCircleResultStateDescriptions).Must(x =>
            x == null
            || x.DistinctBy(y => y.State).Count() == x.Count);
        RuleForEach(v => v.CountingCircleResultStateDescriptions)
            .SetValidator(countingCircleResultStateDescriptionValidator);
    }
}
