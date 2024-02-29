// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Basis.Test.ProtoValidatorTests.Models;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.MajorityElection;

public class ReorderSecondaryMajorityElectionCandidatesRequestTest : ProtoValidatorBaseTest<ReorderSecondaryMajorityElectionCandidatesRequest>
{
    protected override IEnumerable<ReorderSecondaryMajorityElectionCandidatesRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Orders = null);
    }

    protected override IEnumerable<ReorderSecondaryMajorityElectionCandidatesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.SecondaryMajorityElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.SecondaryMajorityElectionId = string.Empty);
    }

    private ReorderSecondaryMajorityElectionCandidatesRequest NewValidRequest(Action<ReorderSecondaryMajorityElectionCandidatesRequest>? action = null)
    {
        var request = new ReorderSecondaryMajorityElectionCandidatesRequest
        {
            SecondaryMajorityElectionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Orders = EntityOrdersTest.NewValid(),
        };

        action?.Invoke(request);
        return request;
    }
}
