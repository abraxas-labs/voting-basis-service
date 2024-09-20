// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.MajorityElection;

public class ListMajorityElectionRequestTest : ProtoValidatorBaseTest<ListMajorityElectionRequest>
{
    protected override IEnumerable<ListMajorityElectionRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ListMajorityElectionRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ContestId = "invalid-guid");
        yield return NewValidRequest(x => x.ContestId = string.Empty);
    }

    private ListMajorityElectionRequest NewValidRequest(Action<ListMajorityElectionRequest>? action = null)
    {
        var request = new ListMajorityElectionRequest
        {
            ContestId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
