// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.MajorityElection;

public class ListMajorityElectionCandidatesRequestTest : ProtoValidatorBaseTest<ListMajorityElectionCandidatesRequest>
{
    protected override IEnumerable<ListMajorityElectionCandidatesRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ListMajorityElectionCandidatesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.MajorityElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.MajorityElectionId = string.Empty);
    }

    private ListMajorityElectionCandidatesRequest NewValidRequest(Action<ListMajorityElectionCandidatesRequest>? action = null)
    {
        var request = new ListMajorityElectionCandidatesRequest
        {
            MajorityElectionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
