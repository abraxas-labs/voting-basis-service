// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Shared.V1;
using Google.Protobuf.WellKnownTypes;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Utils;
using Voting.Lib.Testing.Validation;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class ContestTest : ProtoValidatorBaseTest<ProtoModels.Contest>
{
    public static ProtoModels.Contest NewValid(Action<ProtoModels.Contest>? action = null)
    {
        var contest = new ProtoModels.Contest
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Date = new DateTime(2020, 12, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            Description = { LanguageUtil.MockAllLanguages("test") },
            EndOfTestingPhase = new DateTime(2020, 12, 22, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            DomainOfInfluenceId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            DomainOfInfluence = DomainOfInfluenceTest.NewValid(),
            EVoting = true,
            EVotingFrom = new DateTime(2020, 12, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            EVotingTo = new DateTime(2020, 12, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            PoliticalBusinesses = { PoliticalBusinessTest.NewValid() },
            PoliticalBusinessUnions = { PoliticalBusinessUnionTest.NewValid() },
            State = ContestState.Active,
            PreviousContestId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(contest);
        return contest;
    }

    protected override IEnumerable<ProtoModels.Contest> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(100)));
        yield return NewValid(x => x.EVoting = false);
        yield return NewValid(x => x.EVotingFrom = null);
        yield return NewValid(x => x.EVotingTo = null);
        yield return NewValid(x => x.DomainOfInfluence = null);
        yield return NewValid(x => x.PoliticalBusinesses.Clear());
        yield return NewValid(x => x.PoliticalBusinessUnions.Clear());
        yield return NewValid(x => x.PreviousContestId = string.Empty);
    }

    protected override IEnumerable<ProtoModels.Contest> NotOkMessages()
    {
        yield return NewValid(x => x.Id = "invalid-guid");
        yield return NewValid(x => x.Id = string.Empty);
        yield return NewValid(x => x.Date = null);
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Description, string.Empty, "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Description, "de", string.Empty));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Description, "de", RandomStringUtil.GenerateComplexSingleLineText(101)));
        yield return NewValid(x => x.EndOfTestingPhase = null);
        yield return NewValid(x => x.DomainOfInfluenceId = "invalid-guid");
        yield return NewValid(x => x.DomainOfInfluenceId = string.Empty);
        yield return NewValid(x => x.State = ContestState.Unspecified);
        yield return NewValid(x => x.State = (ContestState)10);
        yield return NewValid(x => x.PreviousContestId = "invalid-guid");
    }
}
