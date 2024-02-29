// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Contest;

public class ListCountingCircleOptionsRequestTest : ProtoValidatorBaseTest<ListCountingCircleOptionsRequest>
{
    protected override IEnumerable<ListCountingCircleOptionsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ListCountingCircleOptionsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Id = "invalid-guid");
        yield return NewValidRequest(x => x.Id = string.Empty);
    }

    private ListCountingCircleOptionsRequest NewValidRequest(Action<ListCountingCircleOptionsRequest>? action = null)
    {
        var request = new ListCountingCircleOptionsRequest
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
