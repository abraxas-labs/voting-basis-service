// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Basis.Test.ProtoValidatorTests.Models;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.ProportionalElection;

public class ReorderProportionalElectionListUnionsRequestTest : ProtoValidatorBaseTest<ReorderProportionalElectionListUnionsRequest>
{
    protected override IEnumerable<ReorderProportionalElectionListUnionsRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.ProportionalElectionRootListUnionId = string.Empty);
        yield return NewValidRequest(x => x.Orders = null);
    }

    protected override IEnumerable<ReorderProportionalElectionListUnionsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionRootListUnionId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionId = string.Empty);
    }

    private ReorderProportionalElectionListUnionsRequest NewValidRequest(Action<ReorderProportionalElectionListUnionsRequest>? action = null)
    {
        var request = new ReorderProportionalElectionListUnionsRequest
        {
            ProportionalElectionRootListUnionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            ProportionalElectionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Orders = EntityOrdersTest.NewValid(),
        };

        action?.Invoke(request);
        return request;
    }
}
