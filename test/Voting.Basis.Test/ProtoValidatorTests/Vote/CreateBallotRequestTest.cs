// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Abraxas.Voting.Basis.Shared.V1;
using Voting.Basis.Test.ProtoValidatorTests.Models;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Vote;

public class CreateBallotRequestTest : ProtoValidatorBaseTest<CreateBallotRequest>
{
    protected override IEnumerable<CreateBallotRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(255)));
        yield return NewValidRequest(x => x.BallotQuestions.Clear());
        yield return NewValidRequest(x => x.HasTieBreakQuestions = false);
        yield return NewValidRequest(x => x.TieBreakQuestions.Clear());
    }

    protected override IEnumerable<CreateBallotRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.VoteId = "invalid-guid");
        yield return NewValidRequest(x => x.VoteId = string.Empty);
        yield return NewValidRequest(x => x.Position = 0);
        yield return NewValidRequest(x => x.Position = 2);
        yield return NewValidRequest(x => x.BallotType = BallotType.Unspecified);
        yield return NewValidRequest(x => x.BallotType = (BallotType)10);
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, string.Empty, "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, "de", string.Empty));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, "de", RandomStringUtil.GenerateComplexSingleLineText(256)));
    }

    private CreateBallotRequest NewValidRequest(Action<CreateBallotRequest>? action = null)
    {
        var request = new CreateBallotRequest
        {
            VoteId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Position = 1,
            BallotType = BallotType.StandardBallot,
            Description = { LanguageUtil.MockAllLanguages("Frage 1") },
            BallotQuestions = { BallotQuestionTest.NewValid() },
            HasTieBreakQuestions = true,
            TieBreakQuestions = { TieBreakQuestionTest.NewValid() },
        };

        action?.Invoke(request);
        return request;
    }
}
