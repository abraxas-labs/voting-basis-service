// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Basis.Data.Models;
using CantonSettingsVotingCardChannel = Voting.Basis.Core.Domain.CantonSettingsVotingCardChannel;

namespace Voting.Basis.Core.Validation;

public class CantonSettingsVotingCardChannelValidator : AbstractValidator<CantonSettingsVotingCardChannel>
{
    public CantonSettingsVotingCardChannelValidator()
    {
        RuleFor(x => x.VotingChannel)
            .IsInEnum()
            .NotEqual(VotingChannel.EVoting) // e voting cannot be directly enabled, it is only enabled via the evoting flag.
            .NotEqual(VotingChannel.Unspecified);

        // invalids are only allowed by mail
        RuleFor(x => x.Valid)
            .Must(x => x)
            .Unless(x => x.VotingChannel == VotingChannel.ByMail);
    }
}
