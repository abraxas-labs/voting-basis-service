// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Vote;

public class UpdateVoteActiveStateRequestTest : ProtoValidatorBaseTest<UpdateVoteActiveStateRequest>
{
    protected override IEnumerable<UpdateVoteActiveStateRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Active = false);
    }

    protected override IEnumerable<UpdateVoteActiveStateRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Id = "invalid-guid");
        yield return NewValidRequest(x => x.Id = string.Empty);
    }

    private UpdateVoteActiveStateRequest NewValidRequest(Action<UpdateVoteActiveStateRequest>? action = null)
    {
        var request = new UpdateVoteActiveStateRequest
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Active = true,
        };

        action?.Invoke(request);
        return request;
    }
}
