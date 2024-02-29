// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.MajorityElection;

public class ListSecondaryMajorityElectionCandidatesRequestTest : ProtoValidatorBaseTest<ListSecondaryMajorityElectionCandidatesRequest>
{
    protected override IEnumerable<ListSecondaryMajorityElectionCandidatesRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ListSecondaryMajorityElectionCandidatesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.SecondaryMajorityElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.SecondaryMajorityElectionId = string.Empty);
    }

    private ListSecondaryMajorityElectionCandidatesRequest NewValidRequest(Action<ListSecondaryMajorityElectionCandidatesRequest>? action = null)
    {
        var request = new ListSecondaryMajorityElectionCandidatesRequest
        {
            SecondaryMajorityElectionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
