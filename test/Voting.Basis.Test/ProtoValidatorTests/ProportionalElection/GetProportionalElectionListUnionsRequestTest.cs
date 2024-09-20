// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.ProportionalElection;

public class GetProportionalElectionListUnionsRequestTest : ProtoValidatorBaseTest<GetProportionalElectionListUnionsRequest>
{
    protected override IEnumerable<GetProportionalElectionListUnionsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetProportionalElectionListUnionsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionId = string.Empty);
    }

    private GetProportionalElectionListUnionsRequest NewValidRequest(Action<GetProportionalElectionListUnionsRequest>? action = null)
    {
        var request = new GetProportionalElectionListUnionsRequest
        {
            ProportionalElectionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
