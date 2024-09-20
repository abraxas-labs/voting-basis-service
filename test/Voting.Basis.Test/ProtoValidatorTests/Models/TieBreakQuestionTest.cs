// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class TieBreakQuestionTest : ProtoValidatorBaseTest<TieBreakQuestion>
{
    public static TieBreakQuestion NewValid(Action<TieBreakQuestion>? action = null)
    {
        var ballotQuestion = new TieBreakQuestion
        {
            Number = 27,
            Question = { LanguageUtil.MockAllLanguages("Frage 1") },
            Question1Number = 1,
            Question2Number = 2,
        };

        action?.Invoke(ballotQuestion);
        return ballotQuestion;
    }

    protected override IEnumerable<TieBreakQuestion> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.Number = 1);
        yield return NewValid(x => x.Number = 50);
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Question, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexMultiLineText(1)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Question, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexMultiLineText(700)));
        yield return NewValid(x => x.Question1Number = 1);
        yield return NewValid(x => x.Question1Number = 50);
        yield return NewValid(x => x.Question2Number = 1);
        yield return NewValid(x => x.Question2Number = 50);
    }

    protected override IEnumerable<TieBreakQuestion> NotOkMessages()
    {
        yield return NewValid(x => x.Number = 0);
        yield return NewValid(x => x.Number = 51);
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Question, string.Empty, "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Question, RandomStringUtil.GenerateComplexMultiLineText(1), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Question, RandomStringUtil.GenerateComplexMultiLineText(3), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Question, "de", string.Empty));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Question, "de", RandomStringUtil.GenerateComplexMultiLineText(701)));
        yield return NewValid(x => x.Question1Number = 0);
        yield return NewValid(x => x.Question1Number = 51);
        yield return NewValid(x => x.Question2Number = 0);
        yield return NewValid(x => x.Question2Number = 51);
    }
}
