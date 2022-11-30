// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.DomainOfInfluence;

public class ListDomainOfInfluenceRequestTest : ProtoValidatorBaseTest<ListDomainOfInfluenceRequest>
{
    protected override IEnumerable<ListDomainOfInfluenceRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.CountingCircleId = string.Empty);
        yield return NewValidRequest(x => x.SecureConnectId = string.Empty);
        yield return NewValidRequest(x => x.SecureConnectId = RandomStringUtil.GenerateNumeric(18));
        yield return NewValidRequest(x => x.SecureConnectId = RandomStringUtil.GenerateNumeric(20));
        yield return NewValidRequest(x => x.ContestDomainOfInfluenceId = string.Empty);
    }

    protected override IEnumerable<ListDomainOfInfluenceRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.CountingCircleId = "invalid-guid");
        yield return NewValidRequest(x => x.SecureConnectId = RandomStringUtil.GenerateNumeric(17));
        yield return NewValidRequest(x => x.SecureConnectId = RandomStringUtil.GenerateNumeric(21));
        yield return NewValidRequest(x => x.SecureConnectId = RandomStringUtil.GenerateAlphabetic(18));
        yield return NewValidRequest(x => x.ContestDomainOfInfluenceId = "invalid-guid");
    }

    private ListDomainOfInfluenceRequest NewValidRequest(Action<ListDomainOfInfluenceRequest>? action = null)
    {
        var request = new ListDomainOfInfluenceRequest
        {
            CountingCircleId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            SecureConnectId = "380590188826699143",
            ContestDomainOfInfluenceId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
