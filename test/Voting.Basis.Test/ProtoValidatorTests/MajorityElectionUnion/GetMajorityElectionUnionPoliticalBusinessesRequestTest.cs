// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.MajorityElectionUnion;

public class GetMajorityElectionUnionPoliticalBusinessesRequestTest : ProtoValidatorBaseTest<GetMajorityElectionUnionPoliticalBusinessesRequest>
{
    protected override IEnumerable<GetMajorityElectionUnionPoliticalBusinessesRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetMajorityElectionUnionPoliticalBusinessesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.MajorityElectionUnionId = "invalid-guid");
        yield return NewValidRequest(x => x.MajorityElectionUnionId = string.Empty);
    }

    private GetMajorityElectionUnionPoliticalBusinessesRequest NewValidRequest(Action<GetMajorityElectionUnionPoliticalBusinessesRequest>? action = null)
    {
        var request = new GetMajorityElectionUnionPoliticalBusinessesRequest
        {
            MajorityElectionUnionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
