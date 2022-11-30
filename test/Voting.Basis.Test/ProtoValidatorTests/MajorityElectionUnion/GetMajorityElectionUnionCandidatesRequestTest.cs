// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.MajorityElectionUnion;

public class GetMajorityElectionUnionCandidatesRequestTest : ProtoValidatorBaseTest<GetMajorityElectionUnionCandidatesRequest>
{
    protected override IEnumerable<GetMajorityElectionUnionCandidatesRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetMajorityElectionUnionCandidatesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.MajorityElectionUnionId = "invalid-guid");
        yield return NewValidRequest(x => x.MajorityElectionUnionId = string.Empty);
    }

    private GetMajorityElectionUnionCandidatesRequest NewValidRequest(Action<GetMajorityElectionUnionCandidatesRequest>? action = null)
    {
        var request = new GetMajorityElectionUnionCandidatesRequest
        {
            MajorityElectionUnionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
