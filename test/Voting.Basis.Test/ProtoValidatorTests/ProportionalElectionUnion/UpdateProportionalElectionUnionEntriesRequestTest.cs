// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.ProportionalElectionUnion;

public class UpdateProportionalElectionUnionEntriesRequestTest : ProtoValidatorBaseTest<UpdateProportionalElectionUnionEntriesRequest>
{
    protected override IEnumerable<UpdateProportionalElectionUnionEntriesRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.ProportionalElectionIds.Clear());
    }

    protected override IEnumerable<UpdateProportionalElectionUnionEntriesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionUnionId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionUnionId = string.Empty);
        yield return NewValidRequest(x => x.ProportionalElectionIds.Add("invalid-guid"));
        yield return NewValidRequest(x => x.ProportionalElectionIds.Add(string.Empty));
    }

    private UpdateProportionalElectionUnionEntriesRequest NewValidRequest(Action<UpdateProportionalElectionUnionEntriesRequest>? action = null)
    {
        var request = new UpdateProportionalElectionUnionEntriesRequest
        {
            ProportionalElectionUnionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            ProportionalElectionIds = { "da36912c-7eaf-43fe-86d4-70c816f17c5a" },
        };

        action?.Invoke(request);
        return request;
    }
}
