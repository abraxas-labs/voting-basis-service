// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class PlausibilisationConfigurationTest : ProtoValidatorBaseTest<PlausibilisationConfiguration>
{
    public static PlausibilisationConfiguration NewValid(Action<PlausibilisationConfiguration>? action = null)
    {
        var plausibilisationConfiguration = new PlausibilisationConfiguration
        {
            ComparisonValidVotingCardsWithAccountedBallotsThresholdPercent = 1.25,
            ComparisonVoterParticipationConfigurations = { ComparisonVoterParticipationConfigurationTest.NewValid() },
            ComparisonVotingChannelConfigurations = { ComparisonVotingChannelConfigurationTest.NewValid() },
            ComparisonCountOfVotersConfigurations = { ComparisonCountOfVotersConfigurationTest.NewValid() },
            ComparisonCountOfVotersCountingCircleEntries = { ComparisonCountOfVotersCountingCircleEntryTest.NewValid() },
        };

        action?.Invoke(plausibilisationConfiguration);
        return plausibilisationConfiguration;
    }

    protected override IEnumerable<PlausibilisationConfiguration> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.ComparisonValidVotingCardsWithAccountedBallotsThresholdPercent = 0.0);
        yield return NewValid(x => x.ComparisonValidVotingCardsWithAccountedBallotsThresholdPercent = 100.0);
        yield return NewValid(x => x.ComparisonValidVotingCardsWithAccountedBallotsThresholdPercent = null);
    }

    protected override IEnumerable<PlausibilisationConfiguration> NotOkMessages()
    {
        yield return NewValid(x => x.ComparisonValidVotingCardsWithAccountedBallotsThresholdPercent = -1);
        yield return NewValid(x => x.ComparisonValidVotingCardsWithAccountedBallotsThresholdPercent = 100.1);
    }
}
