// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.MajorityElectionUnion;

public class UpdateMajorityElectionUnionRequestTest : ProtoValidatorBaseTest<UpdateMajorityElectionUnionRequest>
{
    protected override IEnumerable<UpdateMajorityElectionUnionRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Description = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValidRequest(x => x.Description = RandomStringUtil.GenerateSimpleSingleLineText(50));
    }

    protected override IEnumerable<UpdateMajorityElectionUnionRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Id = "invalid-guid");
        yield return NewValidRequest(x => x.Id = string.Empty);
        yield return NewValidRequest(x => x.Description = string.Empty);
        yield return NewValidRequest(x => x.Description = RandomStringUtil.GenerateSimpleSingleLineText(51));
    }

    private UpdateMajorityElectionUnionRequest NewValidRequest(Action<UpdateMajorityElectionUnionRequest>? action = null)
    {
        var request = new UpdateMajorityElectionUnionRequest
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Description = "description",
        };

        action?.Invoke(request);
        return request;
    }
}
