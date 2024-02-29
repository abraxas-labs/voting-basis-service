// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Vote;

public class GetVoteRequestTest : ProtoValidatorBaseTest<GetVoteRequest>
{
    protected override IEnumerable<GetVoteRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetVoteRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Id = "invalid-guid");
        yield return NewValidRequest(x => x.Id = string.Empty);
    }

    private GetVoteRequest NewValidRequest(Action<GetVoteRequest>? action = null)
    {
        var request = new GetVoteRequest
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
