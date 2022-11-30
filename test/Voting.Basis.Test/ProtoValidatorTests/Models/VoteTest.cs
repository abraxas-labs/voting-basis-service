// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Shared.V1;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Utils;
using Voting.Lib.Testing.Validation;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class VoteTest : ProtoValidatorBaseTest<ProtoModels.Vote>
{
    public static ProtoModels.Vote NewValid(Action<ProtoModels.Vote>? action = null)
    {
        var vote = new ProtoModels.Vote
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            PoliticalBusinessNumber = "1338",
            OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Abstimmung") },
            ShortDescription = { LanguageUtil.MockAllLanguages("Neue Abst") },
            InternalDescription = "Neue Abstimmung",
            DomainOfInfluenceId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            ContestId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Active = true,
            Ballots = { BallotTest.NewValid() },
            ReportDomainOfInfluenceLevel = 1,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            ResultEntry = VoteResultEntry.FinalResults,
            BallotBundleSampleSizePercent = 50,
            AutomaticBallotBundleNumberGeneration = true,
            EnforceResultEntryForCountingCircles = true,
            ReviewProcedure = VoteReviewProcedure.Physically,
            EnforceReviewProcedureForCountingCircles = true,
        };

        action?.Invoke(vote);
        return vote;
    }

    protected override IEnumerable<ProtoModels.Vote> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.PoliticalBusinessNumber = RandomStringUtil.GenerateAlphanumericWhitespace(1));
        yield return NewValid(x => x.PoliticalBusinessNumber = RandomStringUtil.GenerateAlphanumericWhitespace(10));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexMultiLineText(1)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexMultiLineText(700)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(100)));
        yield return NewValid(x => x.InternalDescription = string.Empty);
        yield return NewValid(x => x.InternalDescription = RandomStringUtil.GenerateSimpleSingleLineText(100));
        yield return NewValid(x => x.Active = false);
        yield return NewValid(x => x.Ballots.Clear());
        yield return NewValid(x => x.ReportDomainOfInfluenceLevel = 0);
        yield return NewValid(x => x.ReportDomainOfInfluenceLevel = 10);
        yield return NewValid(x => x.BallotBundleSampleSizePercent = 0);
        yield return NewValid(x => x.BallotBundleSampleSizePercent = 100);
        yield return NewValid(x => x.AutomaticBallotBundleNumberGeneration = false);
        yield return NewValid(x => x.EnforceResultEntryForCountingCircles = false);
        yield return NewValid(x => x.EnforceReviewProcedureForCountingCircles = false);
    }

    protected override IEnumerable<ProtoModels.Vote> NotOkMessages()
    {
        yield return NewValid(x => x.Id = "invalid-guid");
        yield return NewValid(x => x.Id = string.Empty);
        yield return NewValid(x => x.PoliticalBusinessNumber = string.Empty);
        yield return NewValid(x => x.PoliticalBusinessNumber = RandomStringUtil.GenerateAlphanumericWhitespace(11));
        yield return NewValid(x => x.PoliticalBusinessNumber = "9468-12");
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, string.Empty, "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, "de", string.Empty));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, "de", RandomStringUtil.GenerateComplexMultiLineText(701)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, string.Empty, "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, "de", string.Empty));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, "de", RandomStringUtil.GenerateComplexSingleLineText(101)));
        yield return NewValid(x => x.InternalDescription = RandomStringUtil.GenerateSimpleSingleLineText(101));
        yield return NewValid(x => x.InternalDescription = "Neue \nAbstimmung");
        yield return NewValid(x => x.DomainOfInfluenceId = "invalid-guid");
        yield return NewValid(x => x.DomainOfInfluenceId = string.Empty);
        yield return NewValid(x => x.ContestId = "invalid-guid");
        yield return NewValid(x => x.ContestId = string.Empty);
        yield return NewValid(x => x.ReportDomainOfInfluenceLevel = -1);
        yield return NewValid(x => x.ReportDomainOfInfluenceLevel = 11);
        yield return NewValid(x => x.ResultAlgorithm = VoteResultAlgorithm.Unspecified);
        yield return NewValid(x => x.ResultAlgorithm = (VoteResultAlgorithm)10);
        yield return NewValid(x => x.ResultEntry = VoteResultEntry.Unspecified);
        yield return NewValid(x => x.ResultEntry = (VoteResultEntry)10);
        yield return NewValid(x => x.BallotBundleSampleSizePercent = -1);
        yield return NewValid(x => x.BallotBundleSampleSizePercent = 101);
        yield return NewValid(x => x.ReviewProcedure = VoteReviewProcedure.Unspecified);
        yield return NewValid(x => x.ReviewProcedure = (VoteReviewProcedure)10);
    }
}
