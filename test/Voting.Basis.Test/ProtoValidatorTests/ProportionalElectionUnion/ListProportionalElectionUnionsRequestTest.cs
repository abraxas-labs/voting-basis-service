// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.ProportionalElectionUnion;

public class ListProportionalElectionUnionsRequestTest : ProtoValidatorBaseTest<ListProportionalElectionUnionsRequest>
{
    protected override IEnumerable<ListProportionalElectionUnionsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ListProportionalElectionUnionsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionId = string.Empty);
    }

    private ListProportionalElectionUnionsRequest NewValidRequest(Action<ListProportionalElectionUnionsRequest>? action = null)
    {
        var request = new ListProportionalElectionUnionsRequest
        {
            ProportionalElectionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
