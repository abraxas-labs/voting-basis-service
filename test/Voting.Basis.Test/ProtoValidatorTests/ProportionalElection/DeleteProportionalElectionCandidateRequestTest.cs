// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.ProportionalElection;

public class DeleteProportionalElectionCandidateRequestTest : ProtoValidatorBaseTest<DeleteProportionalElectionCandidateRequest>
{
    protected override IEnumerable<DeleteProportionalElectionCandidateRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<DeleteProportionalElectionCandidateRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Id = "invalid-guid");
        yield return NewValidRequest(x => x.Id = string.Empty);
    }

    private DeleteProportionalElectionCandidateRequest NewValidRequest(Action<DeleteProportionalElectionCandidateRequest>? action = null)
    {
        var request = new DeleteProportionalElectionCandidateRequest
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
