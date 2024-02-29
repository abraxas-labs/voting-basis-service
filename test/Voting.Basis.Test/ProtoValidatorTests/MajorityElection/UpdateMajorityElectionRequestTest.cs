// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Abraxas.Voting.Basis.Shared.V1;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.MajorityElection;

public class UpdateMajorityElectionRequestTest : ProtoValidatorBaseTest<UpdateMajorityElectionRequest>
{
    protected override IEnumerable<UpdateMajorityElectionRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.PoliticalBusinessNumber = RandomStringUtil.GenerateAlphanumericWhitespace(1));
        yield return NewValidRequest(x => x.PoliticalBusinessNumber = RandomStringUtil.GenerateAlphanumericWhitespace(10));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexMultiLineText(1)));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexMultiLineText(700)));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(100)));
        yield return NewValidRequest(x => x.NumberOfMandates = 1);
        yield return NewValidRequest(x => x.NumberOfMandates = 100);
        yield return NewValidRequest(x => x.CandidateCheckDigit = false);
        yield return NewValidRequest(x => x.BallotBundleSize = 0);
        yield return NewValidRequest(x => x.BallotBundleSize = 500);
        yield return NewValidRequest(x => x.BallotBundleSampleSize = 0);
        yield return NewValidRequest(x => x.BallotBundleSampleSize = 500);
        yield return NewValidRequest(x => x.AutomaticBallotBundleNumberGeneration = false);
        yield return NewValidRequest(x => x.AutomaticEmptyVoteCounting = false);
        yield return NewValidRequest(x => x.EnforceEmptyVoteCountingForCountingCircles = false);
        yield return NewValidRequest(x => x.EnforceResultEntryForCountingCircles = false);
        yield return NewValidRequest(x => x.Active = false);
        yield return NewValidRequest(x => x.ReportDomainOfInfluenceLevel = 0);
        yield return NewValidRequest(x => x.ReportDomainOfInfluenceLevel = 10);
        yield return NewValidRequest(x => x.EnforceReviewProcedureForCountingCircles = false);
        yield return NewValidRequest(x => x.EnforceCandidateCheckDigitForCountingCircles = false);
    }

    protected override IEnumerable<UpdateMajorityElectionRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Id = "invalid-guid");
        yield return NewValidRequest(x => x.Id = string.Empty);
        yield return NewValidRequest(x => x.PoliticalBusinessNumber = string.Empty);
        yield return NewValidRequest(x => x.PoliticalBusinessNumber = RandomStringUtil.GenerateAlphanumericWhitespace(11));
        yield return NewValidRequest(x => x.PoliticalBusinessNumber = "9468-12");
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, string.Empty, "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, "de", string.Empty));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, "de", RandomStringUtil.GenerateComplexMultiLineText(701)));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, string.Empty, "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, "de", string.Empty));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, "de", RandomStringUtil.GenerateComplexSingleLineText(101)));
        yield return NewValidRequest(x => x.NumberOfMandates = 0);
        yield return NewValidRequest(x => x.NumberOfMandates = 101);
        yield return NewValidRequest(x => x.MandateAlgorithm = MajorityElectionMandateAlgorithm.Unspecified);
        yield return NewValidRequest(x => x.MandateAlgorithm = (MajorityElectionMandateAlgorithm)10);
        yield return NewValidRequest(x => x.BallotBundleSize = -1);
        yield return NewValidRequest(x => x.BallotBundleSize = 501);
        yield return NewValidRequest(x => x.BallotBundleSampleSize = -1);
        yield return NewValidRequest(x => x.BallotBundleSampleSize = 501);
        yield return NewValidRequest(x => x.BallotNumberGeneration = BallotNumberGeneration.Unspecified);
        yield return NewValidRequest(x => x.BallotNumberGeneration = (BallotNumberGeneration)10);
        yield return NewValidRequest(x => x.ResultEntry = MajorityElectionResultEntry.Unspecified);
        yield return NewValidRequest(x => x.ResultEntry = (MajorityElectionResultEntry)10);
        yield return NewValidRequest(x => x.DomainOfInfluenceId = "invalid-guid");
        yield return NewValidRequest(x => x.DomainOfInfluenceId = string.Empty);
        yield return NewValidRequest(x => x.ContestId = "invalid-guid");
        yield return NewValidRequest(x => x.ContestId = string.Empty);
        yield return NewValidRequest(x => x.ReportDomainOfInfluenceLevel = -1);
        yield return NewValidRequest(x => x.ReportDomainOfInfluenceLevel = 11);
        yield return NewValidRequest(x => x.ReviewProcedure = MajorityElectionReviewProcedure.Unspecified);
        yield return NewValidRequest(x => x.ReviewProcedure = (MajorityElectionReviewProcedure)10);
    }

    private UpdateMajorityElectionRequest NewValidRequest(Action<UpdateMajorityElectionRequest>? action = null)
    {
        var request = new UpdateMajorityElectionRequest
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            PoliticalBusinessNumber = "9468",
            OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Majorzwahl") },
            ShortDescription = { LanguageUtil.MockAllLanguages("Neue Majorzwahl") },
            NumberOfMandates = 5,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.AbsoluteMajority,
            CandidateCheckDigit = true,
            BallotBundleSize = 13,
            BallotBundleSampleSize = 1,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.ContinuousForAllBundles,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            ResultEntry = MajorityElectionResultEntry.Detailed,
            EnforceResultEntryForCountingCircles = true,
            DomainOfInfluenceId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            ContestId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Active = true,
            ReportDomainOfInfluenceLevel = 1,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
        };

        action?.Invoke(request);
        return request;
    }
}
