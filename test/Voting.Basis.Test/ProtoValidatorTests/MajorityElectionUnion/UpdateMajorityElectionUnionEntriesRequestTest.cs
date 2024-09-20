// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.MajorityElectionUnion;

public class UpdateMajorityElectionUnionEntriesRequestTest : ProtoValidatorBaseTest<UpdateMajorityElectionUnionEntriesRequest>
{
    protected override IEnumerable<UpdateMajorityElectionUnionEntriesRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.MajorityElectionIds.Clear());
    }

    protected override IEnumerable<UpdateMajorityElectionUnionEntriesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.MajorityElectionUnionId = "invalid-guid");
        yield return NewValidRequest(x => x.MajorityElectionUnionId = string.Empty);
        yield return NewValidRequest(x => x.MajorityElectionIds.Add("invalid-guid"));
        yield return NewValidRequest(x => x.MajorityElectionIds.Add(string.Empty));
    }

    private UpdateMajorityElectionUnionEntriesRequest NewValidRequest(Action<UpdateMajorityElectionUnionEntriesRequest>? action = null)
    {
        var request = new UpdateMajorityElectionUnionEntriesRequest
        {
            MajorityElectionUnionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            MajorityElectionIds = { "da36912c-7eaf-43fe-86d4-70c816f17c5a" },
        };

        action?.Invoke(request);
        return request;
    }
}
