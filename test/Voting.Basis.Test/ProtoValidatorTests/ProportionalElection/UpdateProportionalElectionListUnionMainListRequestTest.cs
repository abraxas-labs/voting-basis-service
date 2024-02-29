// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.ProportionalElection;

public class UpdateProportionalElectionListUnionMainListRequestTest : ProtoValidatorBaseTest<UpdateProportionalElectionListUnionMainListRequest>
{
    protected override IEnumerable<UpdateProportionalElectionListUnionMainListRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.ProportionalElectionMainListId = string.Empty);
    }

    protected override IEnumerable<UpdateProportionalElectionListUnionMainListRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionListUnionId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionListUnionId = string.Empty);
        yield return NewValidRequest(x => x.ProportionalElectionMainListId = "invalid-guid");
    }

    private UpdateProportionalElectionListUnionMainListRequest NewValidRequest(Action<UpdateProportionalElectionListUnionMainListRequest>? action = null)
    {
        var request = new UpdateProportionalElectionListUnionMainListRequest
        {
            ProportionalElectionListUnionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            ProportionalElectionMainListId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
