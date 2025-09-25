// (c) Copyright by Abraxas Informatik AG
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

public class UpdateBallotRequestTest : ProtoValidatorBaseTest<UpdateBallotRequest>
{
    protected override IEnumerable<UpdateBallotRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.BallotQuestions.Clear());
        yield return NewValidRequest(x => x.HasTieBreakQuestions = false);
        yield return NewValidRequest(x => x.TieBreakQuestions.Clear());
        yield return NewValidRequest(x => x.SubType = BallotSubType.MainBallot);
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(100)));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexMultiLineText(1)));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexMultiLineText(255)));
    }

    protected override IEnumerable<UpdateBallotRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Id = "invalid-guid");
        yield return NewValidRequest(x => x.Id = string.Empty);
        yield return NewValidRequest(x => x.VoteId = "invalid-guid");
        yield return NewValidRequest(x => x.VoteId = string.Empty);
        yield return NewValidRequest(x => x.BallotType = BallotType.Unspecified);
        yield return NewValidRequest(x => x.BallotType = (BallotType)10);
        yield return NewValidRequest(x => x.SubType = (BallotSubType)20);
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(101)));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(2), "Short\nDescription"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, string.Empty, RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexMultiLineText(256)));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, string.Empty, RandomStringUtil.GenerateComplexMultiLineText(1)));
    }

    private UpdateBallotRequest NewValidRequest(Action<UpdateBallotRequest>? action = null)
    {
        var request = new UpdateBallotRequest
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            VoteId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            BallotType = BallotType.StandardBallot,
            BallotQuestions = { BallotQuestionTest.NewValid() },
            HasTieBreakQuestions = true,
            TieBreakQuestions = { TieBreakQuestionTest.NewValid() },
        };

        action?.Invoke(request);
        return request;
    }
}
