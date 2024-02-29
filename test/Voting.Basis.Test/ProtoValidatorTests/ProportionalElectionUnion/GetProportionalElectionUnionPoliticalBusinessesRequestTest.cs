// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.ProportionalElectionUnion;

public class GetProportionalElectionUnionPoliticalBusinessesRequestTest : ProtoValidatorBaseTest<GetProportionalElectionUnionPoliticalBusinessesRequest>
{
    protected override IEnumerable<GetProportionalElectionUnionPoliticalBusinessesRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetProportionalElectionUnionPoliticalBusinessesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionUnionId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionUnionId = string.Empty);
    }

    private GetProportionalElectionUnionPoliticalBusinessesRequest NewValidRequest(Action<GetProportionalElectionUnionPoliticalBusinessesRequest>? action = null)
    {
        var request = new GetProportionalElectionUnionPoliticalBusinessesRequest
        {
            ProportionalElectionUnionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
