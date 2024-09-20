// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using FluentValidation;
using Voting.Basis.Data.Models;
using PlausibilisationConfiguration = Voting.Basis.Core.Domain.PlausibilisationConfiguration;

namespace Voting.Basis.Core.Validation;

public class PlausibilisationConfigurationValidator : AbstractValidator<PlausibilisationConfiguration>
{
    public PlausibilisationConfigurationValidator()
    {
        RuleFor(x => x.ComparisonVoterParticipationConfigurations)
            .Must(x => x.Select(y => (y.MainLevel, y.ComparisonLevel)).Distinct().Count() == x.Count);

        RuleFor(x => x.ComparisonVotingChannelConfigurations)
            .Must(x =>
                x.Count == (Enum.GetNames(typeof(VotingChannel)).Length - 1) &&
                x.Select(y => y.VotingChannel).Distinct().Count() == x.Count);

        RuleFor(x => x.ComparisonCountOfVotersConfigurations)
            .Must(x =>
                x.Count == (Enum.GetNames(typeof(ComparisonCountOfVotersCategory)).Length - 1) &&
                x.Select(y => y.Category).Distinct().Count() == x.Count);

        RuleFor(x => x.ComparisonCountOfVotersCountingCircleEntries)
            .Must(x => x.Select(y => y.CountingCircleId).Distinct().Count() == x.Count);
    }
}
