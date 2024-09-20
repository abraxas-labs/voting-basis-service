// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Basis.Test.ProtoValidatorTests.Models;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.ProportionalElection;

public class ReorderProportionalElectionListsRequestTest : ProtoValidatorBaseTest<ReorderProportionalElectionListsRequest>
{
    protected override IEnumerable<ReorderProportionalElectionListsRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Orders = null);
    }

    protected override IEnumerable<ReorderProportionalElectionListsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionId = string.Empty);
    }

    private ReorderProportionalElectionListsRequest NewValidRequest(Action<ReorderProportionalElectionListsRequest>? action = null)
    {
        var request = new ReorderProportionalElectionListsRequest
        {
            ProportionalElectionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Orders = EntityOrdersTest.NewValid(),
        };

        action?.Invoke(request);
        return request;
    }
}
