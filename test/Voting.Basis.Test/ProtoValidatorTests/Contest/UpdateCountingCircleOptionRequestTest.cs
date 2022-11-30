// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Contest;

public class UpdateCountingCircleOptionRequestTest : ProtoValidatorBaseTest<UpdateCountingCircleOptionRequest>
{
    public static UpdateCountingCircleOptionRequest NewValidRequest(Action<UpdateCountingCircleOptionRequest>? action = null)
    {
        var request = new UpdateCountingCircleOptionRequest
        {
            CountingCircleId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            EVoting = true,
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<UpdateCountingCircleOptionRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.EVoting = false);
    }

    protected override IEnumerable<UpdateCountingCircleOptionRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.CountingCircleId = "invalid-guid");
        yield return NewValidRequest(x => x.CountingCircleId = string.Empty);
    }
}
