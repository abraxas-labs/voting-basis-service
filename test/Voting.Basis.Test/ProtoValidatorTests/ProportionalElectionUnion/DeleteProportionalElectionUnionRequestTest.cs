// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.ProportionalElectionUnion;

public class DeleteProportionalElectionUnionRequestTest : ProtoValidatorBaseTest<DeleteProportionalElectionUnionRequest>
{
    protected override IEnumerable<DeleteProportionalElectionUnionRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<DeleteProportionalElectionUnionRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Id = "invalid-guid");
        yield return NewValidRequest(x => x.Id = string.Empty);
    }

    private DeleteProportionalElectionUnionRequest NewValidRequest(Action<DeleteProportionalElectionUnionRequest>? action = null)
    {
        var request = new DeleteProportionalElectionUnionRequest
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
