// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.MajorityElection;

public class UpdateMajorityElectionActiveStateRequestTest : ProtoValidatorBaseTest<UpdateMajorityElectionActiveStateRequest>
{
    protected override IEnumerable<UpdateMajorityElectionActiveStateRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Active = false);
    }

    protected override IEnumerable<UpdateMajorityElectionActiveStateRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Id = "invalid-guid");
        yield return NewValidRequest(x => x.Id = string.Empty);
    }

    private UpdateMajorityElectionActiveStateRequest NewValidRequest(Action<UpdateMajorityElectionActiveStateRequest>? action = null)
    {
        var request = new UpdateMajorityElectionActiveStateRequest
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Active = true,
        };

        action?.Invoke(request);
        return request;
    }
}
