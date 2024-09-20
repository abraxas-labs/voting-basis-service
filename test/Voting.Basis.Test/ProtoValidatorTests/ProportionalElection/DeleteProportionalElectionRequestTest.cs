// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.ProportionalElection;

public class DeleteProportionalElectionRequestTest : ProtoValidatorBaseTest<DeleteProportionalElectionRequest>
{
    protected override IEnumerable<DeleteProportionalElectionRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<DeleteProportionalElectionRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Id = "invalid-guid");
        yield return NewValidRequest(x => x.Id = string.Empty);
    }

    private DeleteProportionalElectionRequest NewValidRequest(Action<DeleteProportionalElectionRequest>? action = null)
    {
        var request = new DeleteProportionalElectionRequest
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
