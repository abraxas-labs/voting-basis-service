// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Vote;

public class DeleteBallotRequestTest : ProtoValidatorBaseTest<DeleteBallotRequest>
{
    protected override IEnumerable<DeleteBallotRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<DeleteBallotRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Id = "invalid-guid");
        yield return NewValidRequest(x => x.Id = string.Empty);
        yield return NewValidRequest(x => x.VoteId = "invalid-guid");
        yield return NewValidRequest(x => x.VoteId = string.Empty);
    }

    private DeleteBallotRequest NewValidRequest(Action<DeleteBallotRequest>? action = null)
    {
        var request = new DeleteBallotRequest
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            VoteId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
