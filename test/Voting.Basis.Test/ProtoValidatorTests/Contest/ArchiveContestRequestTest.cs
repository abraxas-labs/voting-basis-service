// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Google.Protobuf.WellKnownTypes;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Contest;

public class ArchiveContestRequestRequestTest : ProtoValidatorBaseTest<ArchiveContestRequest>
{
    protected override IEnumerable<ArchiveContestRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.ArchivePer = null);
    }

    protected override IEnumerable<ArchiveContestRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Id = "invalid-guid");
        yield return NewValidRequest(x => x.Id = string.Empty);
    }

    private ArchiveContestRequest NewValidRequest(Action<ArchiveContestRequest>? action = null)
    {
        var request = new ArchiveContestRequest
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            ArchivePer = new DateTime(2020, 12, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
        };

        action?.Invoke(request);
        return request;
    }
}
