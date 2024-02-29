// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Abraxas.Voting.Basis.Shared.V1;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class ComparisonVotingChannelConfigurationTest : ProtoValidatorBaseTest<ComparisonVotingChannelConfiguration>
{
    public static ComparisonVotingChannelConfiguration NewValid(Action<ComparisonVotingChannelConfiguration>? action = null)
    {
        var comparisonVotingChannelConfiguration = new ComparisonVotingChannelConfiguration
        {
            VotingChannel = VotingChannel.ByMail,
            ThresholdPercent = 5.5,
        };

        action?.Invoke(comparisonVotingChannelConfiguration);
        return comparisonVotingChannelConfiguration;
    }

    protected override IEnumerable<ComparisonVotingChannelConfiguration> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.ThresholdPercent = 0.0);
        yield return NewValid(x => x.ThresholdPercent = 100.0);
        yield return NewValid(x => x.ThresholdPercent = null);
    }

    protected override IEnumerable<ComparisonVotingChannelConfiguration> NotOkMessages()
    {
        yield return NewValid(x => x.VotingChannel = VotingChannel.Unspecified);
        yield return NewValid(x => x.VotingChannel = (VotingChannel)10);
        yield return NewValid(x => x.ThresholdPercent = -1);
        yield return NewValid(x => x.ThresholdPercent = 100.1);
    }
}
