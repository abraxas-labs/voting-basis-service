// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.ElectionGroup;

public class UpdateElectionGroupRequestTest : ProtoValidatorBaseTest<UpdateElectionGroupRequest>
{
    protected override IEnumerable<UpdateElectionGroupRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Description = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValidRequest(x => x.Description = RandomStringUtil.GenerateSimpleSingleLineText(255));
    }

    protected override IEnumerable<UpdateElectionGroupRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Id = "invalid-guid");
        yield return NewValidRequest(x => x.Id = string.Empty);
        yield return NewValidRequest(x => x.PrimaryMajorityElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.PrimaryMajorityElectionId = string.Empty);
        yield return NewValidRequest(x => x.Description = string.Empty);
        yield return NewValidRequest(x => x.Description = RandomStringUtil.GenerateSimpleSingleLineText(256));
        yield return NewValidRequest(x => x.Description = "New \ndescription");
    }

    private UpdateElectionGroupRequest NewValidRequest(Action<UpdateElectionGroupRequest>? action = null)
    {
        var request = new UpdateElectionGroupRequest
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            PrimaryMajorityElectionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Description = "New description",
        };

        action?.Invoke(request);
        return request;
    }
}
