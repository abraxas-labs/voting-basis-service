// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.MajorityElection;

public class ListMajorityElectionBallotGroupsRequestTest : ProtoValidatorBaseTest<ListMajorityElectionBallotGroupsRequest>
{
    protected override IEnumerable<ListMajorityElectionBallotGroupsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ListMajorityElectionBallotGroupsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.MajorityElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.MajorityElectionId = string.Empty);
    }

    private ListMajorityElectionBallotGroupsRequest NewValidRequest(Action<ListMajorityElectionBallotGroupsRequest>? action = null)
    {
        var request = new ListMajorityElectionBallotGroupsRequest
        {
            MajorityElectionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
