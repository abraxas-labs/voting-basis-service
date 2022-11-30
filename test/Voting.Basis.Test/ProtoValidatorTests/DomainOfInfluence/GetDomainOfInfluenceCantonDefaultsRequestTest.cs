// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.DomainOfInfluence;

public class GetDomainOfInfluenceCantonDefaultsRequestTest : ProtoValidatorBaseTest<GetDomainOfInfluenceCantonDefaultsRequest>
{
    protected override IEnumerable<GetDomainOfInfluenceCantonDefaultsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetDomainOfInfluenceCantonDefaultsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.DomainOfInfluenceId = "invalid-guid");
        yield return NewValidRequest(x => x.DomainOfInfluenceId = string.Empty);
    }

    private GetDomainOfInfluenceCantonDefaultsRequest NewValidRequest(Action<GetDomainOfInfluenceCantonDefaultsRequest>? action = null)
    {
        var request = new GetDomainOfInfluenceCantonDefaultsRequest
        {
            DomainOfInfluenceId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
