// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.ProportionalElection;

public class UpdateProportionalElectionListUnionEntriesRequestTest : ProtoValidatorBaseTest<UpdateProportionalElectionListUnionEntriesRequest>
{
    protected override IEnumerable<UpdateProportionalElectionListUnionEntriesRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.ProportionalElectionListIds.Clear());
    }

    protected override IEnumerable<UpdateProportionalElectionListUnionEntriesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionListUnionId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionListUnionId = string.Empty);
        yield return NewValidRequest(x => x.ProportionalElectionListIds.Add("invalid-guid"));
        yield return NewValidRequest(x => x.ProportionalElectionListIds.Add(string.Empty));
    }

    private UpdateProportionalElectionListUnionEntriesRequest NewValidRequest(Action<UpdateProportionalElectionListUnionEntriesRequest>? action = null)
    {
        var request = new UpdateProportionalElectionListUnionEntriesRequest
        {
            ProportionalElectionListUnionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            ProportionalElectionListIds = { "da36912c-7eaf-43fe-86d4-70c816f17c5a" },
        };

        action?.Invoke(request);
        return request;
    }
}
