// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Google.Protobuf.WellKnownTypes;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Contest;

public class ListContestPastRequestTest : ProtoValidatorBaseTest<ListContestPastRequest>
{
    protected override IEnumerable<ListContestPastRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Date = null);
    }

    protected override IEnumerable<ListContestPastRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.DomainOfInfluenceId = "invalid-guid");
        yield return NewValidRequest(x => x.DomainOfInfluenceId = string.Empty);
    }

    private ListContestPastRequest NewValidRequest(Action<ListContestPastRequest>? action = null)
    {
        var request = new ListContestPastRequest
        {
            DomainOfInfluenceId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Date = new DateTime(2020, 12, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
        };

        action?.Invoke(request);
        return request;
    }
}
