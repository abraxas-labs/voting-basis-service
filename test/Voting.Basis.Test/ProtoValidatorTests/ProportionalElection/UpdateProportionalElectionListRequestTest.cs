// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.ProportionalElection;

public class UpdateProportionalElectionListRequestTest : ProtoValidatorBaseTest<UpdateProportionalElectionListRequest>
{
    protected override IEnumerable<UpdateProportionalElectionListRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.OrderNumber = RandomStringUtil.GenerateAlphanumericWhitespace(1));
        yield return NewValidRequest(x => x.OrderNumber = RandomStringUtil.GenerateAlphanumericWhitespace(4));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(20)));
        yield return NewValidRequest(x => x.BlankRowCount = 0);
        yield return NewValidRequest(x => x.BlankRowCount = 100);
        yield return NewValidRequest(x => x.Position = 1);
        yield return NewValidRequest(x => x.Position = 100);
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(100)));
        yield return NewValidRequest(x => x.PartyId = string.Empty);
    }

    protected override IEnumerable<UpdateProportionalElectionListRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Id = "invalid-guid");
        yield return NewValidRequest(x => x.Id = string.Empty);
        yield return NewValidRequest(x => x.ProportionalElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionId = string.Empty);
        yield return NewValidRequest(x => x.OrderNumber = string.Empty);
        yield return NewValidRequest(x => x.OrderNumber = RandomStringUtil.GenerateAlphanumericWhitespace(5));
        yield return NewValidRequest(x => x.OrderNumber = "num-2");
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, string.Empty, "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, "de", string.Empty));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, "de", RandomStringUtil.GenerateComplexSingleLineText(21)));
        yield return NewValidRequest(x => x.BlankRowCount = -1);
        yield return NewValidRequest(x => x.BlankRowCount = 101);
        yield return NewValidRequest(x => x.Position = 0);
        yield return NewValidRequest(x => x.Position = 101);
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, string.Empty, "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, "de", string.Empty));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, "de", RandomStringUtil.GenerateComplexSingleLineText(101)));
        yield return NewValidRequest(x => x.PartyId = "invalid-guid");
    }

    private UpdateProportionalElectionListRequest NewValidRequest(Action<UpdateProportionalElectionListRequest>? action = null)
    {
        var request = new UpdateProportionalElectionListRequest
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            ProportionalElectionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            OrderNumber = "o1",
            ShortDescription = { LanguageUtil.MockAllLanguages("list") },
            BlankRowCount = 0,
            Position = 1,
            Description = { LanguageUtil.MockAllLanguages("Created list") },
            PartyId = "75b9cee0-f4c2-4463-be32-2a026a7d6508",
        };

        action?.Invoke(request);
        return request;
    }
}
