// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class BallotQuestionTest : ProtoValidatorBaseTest<BallotQuestion>
{
    public static BallotQuestion NewValid(Action<BallotQuestion>? action = null)
    {
        var ballotQuestion = new BallotQuestion
        {
            Number = 27,
            Question = { LanguageUtil.MockAllLanguages("Frage 1") },
        };

        action?.Invoke(ballotQuestion);
        return ballotQuestion;
    }

    protected override IEnumerable<BallotQuestion> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.Number = 1);
        yield return NewValid(x => x.Number = 50);
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Question, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexMultiLineText(1)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Question, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexMultiLineText(700)));
    }

    protected override IEnumerable<BallotQuestion> NotOkMessages()
    {
        yield return NewValid(x => x.Number = 0);
        yield return NewValid(x => x.Number = 51);
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Question, string.Empty, "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Question, RandomStringUtil.GenerateComplexMultiLineText(1), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Question, RandomStringUtil.GenerateComplexMultiLineText(3), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Question, "de", string.Empty));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Question, "de", RandomStringUtil.GenerateComplexMultiLineText(701)));
    }
}
