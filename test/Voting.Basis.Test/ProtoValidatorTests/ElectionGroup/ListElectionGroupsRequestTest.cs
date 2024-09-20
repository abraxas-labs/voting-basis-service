// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.ElectionGroup;

public class ListElectionGroupsRequestTest : ProtoValidatorBaseTest<ListElectionGroupsRequest>
{
    protected override IEnumerable<ListElectionGroupsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ListElectionGroupsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ContestId = "invalid-guid");
        yield return NewValidRequest(x => x.ContestId = string.Empty);
    }

    private ListElectionGroupsRequest NewValidRequest(Action<ListElectionGroupsRequest>? action = null)
    {
        var request = new ListElectionGroupsRequest
        {
            ContestId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
