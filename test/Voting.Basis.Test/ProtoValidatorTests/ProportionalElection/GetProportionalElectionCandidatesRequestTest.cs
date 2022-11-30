// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.ProportionalElection;

public class GetProportionalElectionCandidatesRequestTest : ProtoValidatorBaseTest<GetProportionalElectionCandidatesRequest>
{
    protected override IEnumerable<GetProportionalElectionCandidatesRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetProportionalElectionCandidatesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionListId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionListId = string.Empty);
    }

    private GetProportionalElectionCandidatesRequest NewValidRequest(Action<GetProportionalElectionCandidatesRequest>? action = null)
    {
        var request = new GetProportionalElectionCandidatesRequest
        {
            ProportionalElectionListId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
