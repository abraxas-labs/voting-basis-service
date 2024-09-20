// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Basis.Test.ProtoValidatorTests.Models;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.MajorityElection;

public class ReorderMajorityElectionCandidatesRequestTest : ProtoValidatorBaseTest<ReorderMajorityElectionCandidatesRequest>
{
    protected override IEnumerable<ReorderMajorityElectionCandidatesRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Orders = null);
    }

    protected override IEnumerable<ReorderMajorityElectionCandidatesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.MajorityElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.MajorityElectionId = string.Empty);
    }

    private ReorderMajorityElectionCandidatesRequest NewValidRequest(Action<ReorderMajorityElectionCandidatesRequest>? action = null)
    {
        var request = new ReorderMajorityElectionCandidatesRequest
        {
            MajorityElectionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Orders = EntityOrdersTest.NewValid(),
        };

        action?.Invoke(request);
        return request;
    }
}
