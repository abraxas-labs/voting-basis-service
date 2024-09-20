// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Google.Protobuf.WellKnownTypes;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.DomainOfInfluence;

public class ListDomainOfInfluenceSnapshotRequestTest : ProtoValidatorBaseTest<ListDomainOfInfluenceSnapshotRequest>
{
    protected override IEnumerable<ListDomainOfInfluenceSnapshotRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.DateTime = null);
    }

    protected override IEnumerable<ListDomainOfInfluenceSnapshotRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.CountingCircleId = "invalid-guid");
        yield return NewValidRequest(x => x.CountingCircleId = string.Empty);
    }

    private ListDomainOfInfluenceSnapshotRequest NewValidRequest(Action<ListDomainOfInfluenceSnapshotRequest>? action = null)
    {
        var request = new ListDomainOfInfluenceSnapshotRequest
        {
            CountingCircleId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            DateTime = new DateTime(2020, 12, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
        };

        action?.Invoke(request);
        return request;
    }
}
