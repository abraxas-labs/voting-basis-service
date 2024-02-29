// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Abraxas.Voting.Basis.Shared.V1;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class CantonSettingsVotingCardChannelTest : ProtoValidatorBaseTest<CantonSettingsVotingCardChannel>
{
    public static CantonSettingsVotingCardChannel NewValid(Action<CantonSettingsVotingCardChannel>? action = null)
    {
        var cantonSettingsVotingCardChannel = new CantonSettingsVotingCardChannel
        {
            Valid = true,
            VotingChannel = VotingChannel.EVoting,
        };

        action?.Invoke(cantonSettingsVotingCardChannel);
        return cantonSettingsVotingCardChannel;
    }

    protected override IEnumerable<CantonSettingsVotingCardChannel> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.Valid = false);
    }

    protected override IEnumerable<CantonSettingsVotingCardChannel> NotOkMessages()
    {
        yield return NewValid(x => x.VotingChannel = VotingChannel.Unspecified);
        yield return NewValid(x => x.VotingChannel = (VotingChannel)10);
    }
}
