// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.ProportionalElection;

public class GetProportionalElectionListsRequestTest : ProtoValidatorBaseTest<GetProportionalElectionListsRequest>
{
    protected override IEnumerable<GetProportionalElectionListsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetProportionalElectionListsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionId = string.Empty);
    }

    private GetProportionalElectionListsRequest NewValidRequest(Action<GetProportionalElectionListsRequest>? action = null)
    {
        var request = new GetProportionalElectionListsRequest
        {
            ProportionalElectionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
