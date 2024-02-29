// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Shared.V1;
using Voting.Lib.Testing.Validation;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class BallotTest : ProtoValidatorBaseTest<ProtoModels.Ballot>
{
    public static ProtoModels.Ballot NewValid(Action<ProtoModels.Ballot>? action = null)
    {
        var ballot = new ProtoModels.Ballot
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Position = 1,
            BallotType = BallotType.StandardBallot,
            BallotQuestions = { BallotQuestionTest.NewValid() },
            VoteId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            HasTieBreakQuestions = true,
            TieBreakQuestions = { TieBreakQuestionTest.NewValid() },
        };

        action?.Invoke(ballot);
        return ballot;
    }

    protected override IEnumerable<ProtoModels.Ballot> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.BallotQuestions.Clear());
        yield return NewValid(x => x.HasTieBreakQuestions = false);
        yield return NewValid(x => x.TieBreakQuestions.Clear());
    }

    protected override IEnumerable<ProtoModels.Ballot> NotOkMessages()
    {
        yield return NewValid(x => x.Id = "invalid-guid");
        yield return NewValid(x => x.Id = string.Empty);
        yield return NewValid(x => x.Position = 0);
        yield return NewValid(x => x.Position = 51);
        yield return NewValid(x => x.BallotType = BallotType.Unspecified);
        yield return NewValid(x => x.BallotType = (BallotType)10);
        yield return NewValid(x => x.VoteId = "invalid-guid");
        yield return NewValid(x => x.VoteId = string.Empty);
    }
}
