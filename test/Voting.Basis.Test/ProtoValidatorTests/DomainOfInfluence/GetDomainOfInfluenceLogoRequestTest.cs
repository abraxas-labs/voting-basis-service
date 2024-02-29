// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.DomainOfInfluence;

public class GetDomainOfInfluenceLogoRequestTest : ProtoValidatorBaseTest<GetDomainOfInfluenceLogoRequest>
{
    protected override IEnumerable<GetDomainOfInfluenceLogoRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetDomainOfInfluenceLogoRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.DomainOfInfluenceId = "invalid-guid");
        yield return NewValidRequest(x => x.DomainOfInfluenceId = string.Empty);
    }

    private GetDomainOfInfluenceLogoRequest NewValidRequest(Action<GetDomainOfInfluenceLogoRequest>? action = null)
    {
        var request = new GetDomainOfInfluenceLogoRequest
        {
            DomainOfInfluenceId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
