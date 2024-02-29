// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.MajorityElection;

public class GetMajorityElectionCandidateRequestTest : ProtoValidatorBaseTest<GetMajorityElectionCandidateRequest>
{
    protected override IEnumerable<GetMajorityElectionCandidateRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetMajorityElectionCandidateRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Id = "invalid-guid");
        yield return NewValidRequest(x => x.Id = string.Empty);
    }

    private GetMajorityElectionCandidateRequest NewValidRequest(Action<GetMajorityElectionCandidateRequest>? action = null)
    {
        var request = new GetMajorityElectionCandidateRequest
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
