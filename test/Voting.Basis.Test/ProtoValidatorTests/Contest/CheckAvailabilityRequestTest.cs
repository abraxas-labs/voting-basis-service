// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Google.Protobuf.WellKnownTypes;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Contest;

public class CheckAvailabilityRequestTest : ProtoValidatorBaseTest<CheckAvailabilityRequest>
{
    protected override IEnumerable<CheckAvailabilityRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<CheckAvailabilityRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Date = null);
        yield return NewValidRequest(x => x.DomainOfInfluenceId = "invalid-guid");
        yield return NewValidRequest(x => x.DomainOfInfluenceId = string.Empty);
    }

    private CheckAvailabilityRequest NewValidRequest(Action<CheckAvailabilityRequest>? action = null)
    {
        var request = new CheckAvailabilityRequest
        {
            Date = new DateTime(2020, 12, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            DomainOfInfluenceId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
