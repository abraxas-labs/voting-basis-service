// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Basis.Test.ProtoValidatorTests.Models;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.CountingCircle;

public class CreateCountingCircleRequestTest : ProtoValidatorBaseTest<CreateCountingCircleRequest>
{
    protected override IEnumerable<CreateCountingCircleRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Name = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValidRequest(x => x.Name = RandomStringUtil.GenerateSimpleSingleLineText(100));
        yield return NewValidRequest(x => x.Bfs = RandomStringUtil.GenerateAlphanumericWhitespace(1));
        yield return NewValidRequest(x => x.Bfs = RandomStringUtil.GenerateAlphanumericWhitespace(8));
        yield return NewValidRequest(x => x.ContactPersonSameDuringEventAsAfter = false);
        yield return NewValidRequest(x => x.Code = string.Empty);
        yield return NewValidRequest(x => x.Code = RandomStringUtil.GenerateAlphanumericWhitespace(1));
        yield return NewValidRequest(x => x.Code = RandomStringUtil.GenerateAlphanumericWhitespace(12));
        yield return NewValidRequest(x => x.SortNumber = 0);
        yield return NewValidRequest(x => x.SortNumber = 1000);
        yield return NewValidRequest(x => x.NameForProtocol = string.Empty);
        yield return NewValidRequest(x => x.NameForProtocol = RandomStringUtil.GenerateComplexSingleLineText(100));
    }

    protected override IEnumerable<CreateCountingCircleRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Name = string.Empty);
        yield return NewValidRequest(x => x.Name = RandomStringUtil.GenerateSimpleSingleLineText(101));
        yield return NewValidRequest(x => x.Name = "Kreis\n 1");
        yield return NewValidRequest(x => x.Bfs = string.Empty);
        yield return NewValidRequest(x => x.Bfs = RandomStringUtil.GenerateAlphanumericWhitespace(9));
        yield return NewValidRequest(x => x.Bfs = "1234-56");
        yield return NewValidRequest(x => x.Code = RandomStringUtil.GenerateAlphanumericWhitespace(13));
        yield return NewValidRequest(x => x.Code = "1234-56");
        yield return NewValidRequest(x => x.SortNumber = -1);
        yield return NewValidRequest(x => x.SortNumber = 1001);
        yield return NewValidRequest(x => x.NameForProtocol = RandomStringUtil.GenerateComplexSingleLineText(101));
        yield return NewValidRequest(x => x.NameForProtocol = "Kreis 1 \n(Protokoll)");
    }

    private CreateCountingCircleRequest NewValidRequest(Action<CreateCountingCircleRequest>? action = null)
    {
        var request = new CreateCountingCircleRequest
        {
            Name = "Kreis 1",
            Bfs = "1234",
            ResponsibleAuthority = AuthorityTest.NewValid(),
            ContactPersonDuringEvent = ContactPersonTest.NewValid(),
            ContactPersonSameDuringEventAsAfter = true,
            Code = "12345",
            SortNumber = 5,
            NameForProtocol = "Kreis 1 (Protokoll)",
            Electorates = { CountingCircleElectorateTest.NewValid() },
        };

        action?.Invoke(request);
        return request;
    }
}
