// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Google.Protobuf.WellKnownTypes;
using Voting.Basis.Test.ProtoValidatorTests.Models;
using Voting.Lib.Testing.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.CountingCircle;

public class UpdateScheduledCountingCirclesMergerRequestTest : ProtoValidatorBaseTest<UpdateScheduledCountingCirclesMergerRequest>
{
    protected override IEnumerable<UpdateScheduledCountingCirclesMergerRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Name = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValidRequest(x => x.Name = RandomStringUtil.GenerateSimpleSingleLineText(100));
        yield return NewValidRequest(x => x.Bfs = RandomStringUtil.GenerateAlphanumericWhitespace(1));
        yield return NewValidRequest(x => x.Bfs = RandomStringUtil.GenerateAlphanumericWhitespace(8));
        yield return NewValidRequest(x => x.Code = string.Empty);
        yield return NewValidRequest(x => x.Code = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValidRequest(x => x.Code = RandomStringUtil.GenerateSimpleSingleLineText(20));
        yield return NewValidRequest(x => x.MergedCountingCircleIds.Clear());
        yield return NewValidRequest(x => x.SortNumber = 0);
        yield return NewValidRequest(x => x.SortNumber = 1000);
        yield return NewValidRequest(x => x.NameForProtocol = string.Empty);
        yield return NewValidRequest(x => x.NameForProtocol = RandomStringUtil.GenerateComplexSingleLineText(100));
    }

    protected override IEnumerable<UpdateScheduledCountingCirclesMergerRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.NewCountingCircleId = "invalid-guid");
        yield return NewValidRequest(x => x.NewCountingCircleId = string.Empty);
        yield return NewValidRequest(x => x.Name = string.Empty);
        yield return NewValidRequest(x => x.Name = RandomStringUtil.GenerateSimpleSingleLineText(101));
        yield return NewValidRequest(x => x.Name = "Kreis\n 1");
        yield return NewValidRequest(x => x.Bfs = string.Empty);
        yield return NewValidRequest(x => x.Bfs = RandomStringUtil.GenerateAlphanumericWhitespace(9));
        yield return NewValidRequest(x => x.Bfs = "1234-56");
        yield return NewValidRequest(x => x.Code = RandomStringUtil.GenerateSimpleSingleLineText(21));
        yield return NewValidRequest(x => x.Code = "1234_56");
        yield return NewValidRequest(x => x.MergedCountingCircleIds.Add("invalid-guid"));
        yield return NewValidRequest(x => x.MergedCountingCircleIds.Add(string.Empty));
        yield return NewValidRequest(x => x.CopyFromCountingCircleId = "invalid-guid");
        yield return NewValidRequest(x => x.CopyFromCountingCircleId = string.Empty);
        yield return NewValidRequest(x => x.ActiveFrom = null);
        yield return NewValidRequest(x => x.SortNumber = -1);
        yield return NewValidRequest(x => x.SortNumber = 1001);
        yield return NewValidRequest(x => x.NameForProtocol = RandomStringUtil.GenerateComplexSingleLineText(101));
        yield return NewValidRequest(x => x.NameForProtocol = "Kreis 1 \n(Protokoll)");
    }

    private UpdateScheduledCountingCirclesMergerRequest NewValidRequest(Action<UpdateScheduledCountingCirclesMergerRequest>? action = null)
    {
        var request = new UpdateScheduledCountingCirclesMergerRequest
        {
            NewCountingCircleId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Name = "Kreis 1",
            Bfs = "1234",
            Code = "12345",
            ResponsibleAuthority = AuthorityTest.NewValid(),
            MergedCountingCircleIds = { "da36912c-7eaf-43fe-86d4-70c816f17c5a", "da36912c-7eaf-43fe-86d4-70c816f17c5a" },
            CopyFromCountingCircleId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            ActiveFrom = new DateTime(2020, 12, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            SortNumber = 5,
            NameForProtocol = "Kreis 1 (Protokoll)",
        };

        action?.Invoke(request);
        return request;
    }
}
