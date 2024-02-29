// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.ProportionalElectionUnion;

public class GetProportionalElectionUnionCandidatesRequestTest : ProtoValidatorBaseTest<GetProportionalElectionUnionCandidatesRequest>
{
    protected override IEnumerable<GetProportionalElectionUnionCandidatesRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetProportionalElectionUnionCandidatesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionUnionId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionUnionId = string.Empty);
    }

    private GetProportionalElectionUnionCandidatesRequest NewValidRequest(Action<GetProportionalElectionUnionCandidatesRequest>? action = null)
    {
        var request = new GetProportionalElectionUnionCandidatesRequest
        {
            ProportionalElectionUnionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
