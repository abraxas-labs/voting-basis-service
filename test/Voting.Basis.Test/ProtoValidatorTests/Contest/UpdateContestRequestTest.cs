// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Google.Protobuf.WellKnownTypes;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Contest;

public class UpdateContestRequestTest : ProtoValidatorBaseTest<UpdateContestRequest>
{
    protected override IEnumerable<UpdateContestRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(100)));
        yield return NewValidRequest(x => x.EVoting = false);
        yield return NewValidRequest(x => x.EVotingFrom = null);
        yield return NewValidRequest(x => x.EVotingTo = null);
        yield return NewValidRequest(x => x.PreviousContestId = string.Empty);
    }

    protected override IEnumerable<UpdateContestRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Id = "invalid-guid");
        yield return NewValidRequest(x => x.Id = string.Empty);
        yield return NewValidRequest(x => x.Date = null);
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, string.Empty, "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, "de", string.Empty));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, "de", RandomStringUtil.GenerateComplexSingleLineText(101)));
        yield return NewValidRequest(x => x.EndOfTestingPhase = null);
        yield return NewValidRequest(x => x.DomainOfInfluenceId = "invalid-guid");
        yield return NewValidRequest(x => x.DomainOfInfluenceId = string.Empty);
        yield return NewValidRequest(x => x.PreviousContestId = "invalid-guid");
    }

    private UpdateContestRequest NewValidRequest(Action<UpdateContestRequest>? action = null)
    {
        var request = new UpdateContestRequest
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Date = new DateTime(2020, 12, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            Description = { LanguageUtil.MockAllLanguages("test") },
            EndOfTestingPhase = new DateTime(2020, 12, 22, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            DomainOfInfluenceId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            EVoting = true,
            EVotingFrom = new DateTime(2020, 12, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            EVotingTo = new DateTime(2020, 12, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            PreviousContestId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
