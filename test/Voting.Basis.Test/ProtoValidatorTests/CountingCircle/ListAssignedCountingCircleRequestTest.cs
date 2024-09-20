// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.CountingCircle;

public class ListAssignedCountingCircleRequestTest : ProtoValidatorBaseTest<ListAssignedCountingCircleRequest>
{
    protected override IEnumerable<ListAssignedCountingCircleRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ListAssignedCountingCircleRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.DomainOfInfluenceId = "invalid-guid");
        yield return NewValidRequest(x => x.DomainOfInfluenceId = string.Empty);
    }

    private ListAssignedCountingCircleRequest NewValidRequest(Action<ListAssignedCountingCircleRequest>? action = null)
    {
        var request = new ListAssignedCountingCircleRequest
        {
            DomainOfInfluenceId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
