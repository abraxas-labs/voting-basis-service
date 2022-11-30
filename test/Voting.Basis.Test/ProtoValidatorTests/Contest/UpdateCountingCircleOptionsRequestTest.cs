// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Contest;

public class UpdateCountingCircleOptionsRequestTest : ProtoValidatorBaseTest<UpdateCountingCircleOptionsRequest>
{
    protected override IEnumerable<UpdateCountingCircleOptionsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<UpdateCountingCircleOptionsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Id = "invalid-guid");
        yield return NewValidRequest(x => x.Id = string.Empty);
    }

    private UpdateCountingCircleOptionsRequest NewValidRequest(Action<UpdateCountingCircleOptionsRequest>? action = null)
    {
        var request = new UpdateCountingCircleOptionsRequest
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Options = { UpdateCountingCircleOptionRequestTest.NewValidRequest() },
        };

        action?.Invoke(request);
        return request;
    }
}
