// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.ProportionalElectionUnion;

public class CreateProportionalElectionUnionRequestTest : ProtoValidatorBaseTest<CreateProportionalElectionUnionRequest>
{
    protected override IEnumerable<CreateProportionalElectionUnionRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Description = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValidRequest(x => x.Description = RandomStringUtil.GenerateSimpleSingleLineText(50));
    }

    protected override IEnumerable<CreateProportionalElectionUnionRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ContestId = "invalid-guid");
        yield return NewValidRequest(x => x.ContestId = string.Empty);
        yield return NewValidRequest(x => x.Description = string.Empty);
        yield return NewValidRequest(x => x.Description = RandomStringUtil.GenerateSimpleSingleLineText(51));
    }

    private CreateProportionalElectionUnionRequest NewValidRequest(Action<CreateProportionalElectionUnionRequest>? action = null)
    {
        var request = new CreateProportionalElectionUnionRequest
        {
            ContestId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Description = "description",
        };

        action?.Invoke(request);
        return request;
    }
}
