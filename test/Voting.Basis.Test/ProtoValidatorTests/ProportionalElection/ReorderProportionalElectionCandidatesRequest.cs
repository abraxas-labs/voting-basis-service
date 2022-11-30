// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Basis.Test.ProtoValidatorTests.Models;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.ProportionalElection;

public class ReorderProportionalElectionCandidatesRequestTest : ProtoValidatorBaseTest<ReorderProportionalElectionCandidatesRequest>
{
    protected override IEnumerable<ReorderProportionalElectionCandidatesRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Orders = null);
    }

    protected override IEnumerable<ReorderProportionalElectionCandidatesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionListId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionListId = string.Empty);
    }

    private ReorderProportionalElectionCandidatesRequest NewValidRequest(Action<ReorderProportionalElectionCandidatesRequest>? action = null)
    {
        var request = new ReorderProportionalElectionCandidatesRequest
        {
            ProportionalElectionListId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Orders = EntityOrdersTest.NewValid(),
        };

        action?.Invoke(request);
        return request;
    }
}
