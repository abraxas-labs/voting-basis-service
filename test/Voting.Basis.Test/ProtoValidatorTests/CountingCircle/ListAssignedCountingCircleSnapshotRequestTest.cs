// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Google.Protobuf.WellKnownTypes;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.CountingCircle;

public class ListAssignedCountingCircleSnapshotRequestTest : ProtoValidatorBaseTest<ListAssignedCountingCircleSnapshotRequest>
{
    protected override IEnumerable<ListAssignedCountingCircleSnapshotRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ListAssignedCountingCircleSnapshotRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.DomainOfInfluenceId = "invalid-guid");
        yield return NewValidRequest(x => x.DomainOfInfluenceId = string.Empty);
    }

    private ListAssignedCountingCircleSnapshotRequest NewValidRequest(Action<ListAssignedCountingCircleSnapshotRequest>? action = null)
    {
        var request = new ListAssignedCountingCircleSnapshotRequest
        {
            DomainOfInfluenceId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            DateTime = new DateTime(2020, 12, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
        };

        action?.Invoke(request);
        return request;
    }
}
