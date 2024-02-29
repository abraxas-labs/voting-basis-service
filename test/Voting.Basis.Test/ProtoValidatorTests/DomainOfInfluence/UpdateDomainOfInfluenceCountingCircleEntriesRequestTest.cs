// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.DomainOfInfluence;

public class UpdateDomainOfInfluenceCountingCircleEntriesRequestTest : ProtoValidatorBaseTest<UpdateDomainOfInfluenceCountingCircleEntriesRequest>
{
    protected override IEnumerable<UpdateDomainOfInfluenceCountingCircleEntriesRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.CountingCircleIds.Clear());
    }

    protected override IEnumerable<UpdateDomainOfInfluenceCountingCircleEntriesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Id = "invalid-guid");
        yield return NewValidRequest(x => x.Id = string.Empty);
        yield return NewValidRequest(x => x.CountingCircleIds.Add("invalid-guid"));
        yield return NewValidRequest(x => x.CountingCircleIds.Add(string.Empty));
    }

    private UpdateDomainOfInfluenceCountingCircleEntriesRequest NewValidRequest(Action<UpdateDomainOfInfluenceCountingCircleEntriesRequest>? action = null)
    {
        var request = new UpdateDomainOfInfluenceCountingCircleEntriesRequest
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            CountingCircleIds = { "da36912c-7eaf-43fe-86d4-70c816f17c5a" },
        };

        action?.Invoke(request);
        return request;
    }
}
