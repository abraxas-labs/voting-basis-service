// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.MajorityElectionUnion;

public class CreateMajorityElectionUnionRequestTest : ProtoValidatorBaseTest<CreateMajorityElectionUnionRequest>
{
    protected override IEnumerable<CreateMajorityElectionUnionRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Description = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValidRequest(x => x.Description = RandomStringUtil.GenerateSimpleSingleLineText(50));
    }

    protected override IEnumerable<CreateMajorityElectionUnionRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ContestId = "invalid-guid");
        yield return NewValidRequest(x => x.ContestId = string.Empty);
        yield return NewValidRequest(x => x.Description = string.Empty);
        yield return NewValidRequest(x => x.Description = RandomStringUtil.GenerateSimpleSingleLineText(51));
    }

    private CreateMajorityElectionUnionRequest NewValidRequest(Action<CreateMajorityElectionUnionRequest>? action = null)
    {
        var request = new CreateMajorityElectionUnionRequest
        {
            ContestId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Description = "description",
        };

        action?.Invoke(request);
        return request;
    }
}
