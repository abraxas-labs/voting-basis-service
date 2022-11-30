// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.ProportionalElectionUnion;

public class GetProportionalElectionUnionListsRequestTest : ProtoValidatorBaseTest<GetProportionalElectionUnionListsRequest>
{
    protected override IEnumerable<GetProportionalElectionUnionListsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetProportionalElectionUnionListsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionUnionId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionUnionId = string.Empty);
    }

    private GetProportionalElectionUnionListsRequest NewValidRequest(Action<GetProportionalElectionUnionListsRequest>? action = null)
    {
        var request = new GetProportionalElectionUnionListsRequest
        {
            ProportionalElectionUnionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
