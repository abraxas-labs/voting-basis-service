// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.EventProcessors;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Store;
using Voting.Lib.Testing.Utils;
using ElectionGroup = Voting.Basis.Data.Models.ElectionGroup;
using MajorityElection = Voting.Basis.Data.Models.MajorityElection;
using MajorityElectionBallotGroup = Voting.Basis.Data.Models.MajorityElectionBallotGroup;
using MajorityElectionBallotGroupEntry = Voting.Basis.Data.Models.MajorityElectionBallotGroupEntry;
using MajorityElectionCandidate = Voting.Basis.Data.Models.MajorityElectionCandidate;
using SecondaryMajorityElection = Voting.Basis.Data.Models.SecondaryMajorityElection;

namespace Voting.Basis.Test.MockedData;

public static class MajorityElectionMockedData
{
    public const string IdBundMajorityElectionInContestBund = "7566c420-3774-4c57-9b31-9702fac37543";
    public const string IdStGallenMajorityElectionInContestBund = "b0da46f8-a721-4e1a-ac36-25284d68f34b";
    public const string IdUzwilMajorityElectionInContestBund = "7ae77f44-1083-470a-bb66-64f921bc6945";
    public const string IdBundMajorityElectionInContestStGallen = "0fee4b4e-f16d-46a3-9f7a-2776ee5785db";
    public const string IdGossauMajorityElectionInContestStGallen = "50415df8-6ee9-4eb4-9e31-68c0d3021e76";
    public const string IdGossauMajorityElectionInContestBund = "e74b6879-9e2b-4799-a156-d0321b967dcf";
    public const string IdUzwilMajorityElectionInContestStGallen = "d66ced3e-a2e4-4178-932b-ac91ee6a9d85";
    public const string IdStGallenMajorityElectionInContestStGallen = "cd464c26-24d4-4cfc-95d9-e7c930b1784e";
    public const string IdStGallenMajorityElectionInContestStGallenWithoutChilds = "a6ab97b9-ce86-4973-876e-a128ff279bf7";
    public const string IdGossauMajorityElectionInContestGossau = "e39a4d1c-6db4-44a7-a707-05cf2005dd4a";
    public const string IdUzwilMajorityElectionInContestUzwilWithoutChilds = "4aebd757-9f88-4a76-90b4-497dc64adb6f";
    public const string IdGenfMajorityElectionInContestBundWithoutChilds = "2c3fe189-99a2-401a-8af6-8ac5f1bf3c3a";
    public const string IdKircheMajorityElectionInContestKirche = "65ec16ca-81cf-4c8d-9ee3-f741744c31fb";
    public const string IdKircheMajorityElectionInContestKircheWithoutChilds = "a24c7ec8-bca9-4c66-9030-dc2539fd1c06";

    public const string CandidateIdBundMajorityElectionInContestBund = "94a02a0c-b654-4917-92a0-f6fe3fa05799";
    public const string CandidateId1StGallenMajorityElectionInContestBund = "81a11b8e-51b8-40c5-aa94-b7a854e2c726";
    public const string CandidateId2StGallenMajorityElectionInContestBund = "efdbb5e3-16bf-4a53-95c3-a35ed6371819";
    public const string CandidateId1BundMajorityElectionInContestStGallen = "665f752b-6248-4f06-a232-6c7af148e550";
    public const string CandidateId2BundMajorityElectionInContestStGallen = "c966085a-5cba-499b-bcf0-788e13ad0984";
    public const string CandidateIdUzwilMajorityElectionInContestStGallen = "be81a3f3-5a9e-4a69-8f19-4f0598a32955";
    public const string CandidateIdStGallenMajorityElectionInContestStGallen = "1228f95d-8b39-44b1-8cc3-84a93f5e3bbc";
    public const string CandidateId1GossauMajorityElectionInContestStGallen = "77ce6bf0-b27c-4d9d-926f-ced7863aff2f";
    public const string CandidateId2GossauMajorityElectionInContestStGallen = "4a44cb35-05a7-41f9-aa1e-034bedd320ec";
    public const string CandidateId1GossauMajorityElectionInContestBund = "f5d75dbd-8a22-4fa8-b580-f0c1ed965303";
    public const string CandidateId2GossauMajorityElectionInContestBund = "9c2a6284-a83d-44e6-88aa-676616e4a774";
    public const string CandidateIdGossauMajorityElectionInContestGossau = "194ff485-6eb9-4d98-8bec-855a2ec92650";
    public const string CandidateIdUzwilMajorityElectionInContestUzwil = "3be5ce95-56db-424d-ab11-6fbb18196862";
    public const string CandidateIdKircheMajorityElectionInContestKirche = "56cd0f70-9976-4efb-b09d-d6e60fa03904";

    public const string SecondaryElectionIdStGallenMajorityElectionInContestBund = "0741da26-add2-4c4c-960d-7e251b82e91b";
    public const string SecondaryElectionIdGossauMajorityElectionInContestBund = "654455b1-85a1-44c2-ad7b-8a8e7ea29d95";
    public const string SecondaryElectionIdGossauMajorityElectionInContestStGallen = "b321b60c-7aa2-4811-87a3-fffb426af1d6";
    public const string SecondaryElectionIdUzwilMajorityElectionInContestStGallen = "5ec8cf85-229a-4dc7-a601-1bb6980fee78";
    public const string SecondaryElectionIdKircheMajorityElectionInContestKirche = "12a6adbe-8872-4baa-982d-a4a0dae93834";

    public const string SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund = "9d1e4ef4-81c0-4905-a2c6-e1b937b80ddf";
    public const string SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund = "f70ffba1-1cfd-402b-b6ea-f2c9f802886a";
    public const string SecondaryElectionCandidateId1GossauMajorityElectionInContestBund = "fb41851b-7cfd-4c0d-9af0-e5f1ced6569c";
    public const string SecondaryElectionCandidateId2GossauMajorityElectionInContestBund = "50eae8b0-53a0-44d1-a8c0-bdc28405a9c2";
    public const string SecondaryElectionCandidateIdUzwilMajorityElectionInContestStGallen = "6bf76024-ed15-4b7e-b68e-583b7bb5d2d5";
    public const string SecondaryElectionCandidateIdKircheMajorityElectionInContestKirche = "30e50602-ad94-40bf-b8ff-0f91c82d20b1";
    public const string SecondaryElectionCandidateId1GossauMajorityElectionInContestStGallen = "386bd391-9ec0-49d1-802c-0fd8e755ac8a";
    public const string SecondaryElectionCandidateId2GossauMajorityElectionInContestStGallen = "a4d71ea8-0968-4684-bf5a-cb6834233dbe";
    public const string SecondaryElectionCandidateId3GossauMajorityElectionInContestStGallen = "6050fa7f-68c6-4ada-95f6-2274f5f2770e";

    public const string ElectionGroupIdStGallenMajorityElectionInContestBund = "63bf2387-08e0-45ed-96e6-263f85500e28";
    public const string ElectionGroupIdGossauMajorityElectionInContestBund = "85caefc1-e01a-458a-a0ce-1aeff0b43f86";
    public const string ElectionGroupIdUzwilMajorityElectionInContestStGallen = "6eaa4d6b-eb0a-4315-b0b8-a68266303ab1";
    public const string ElectionGroupIdKircheMajorityElectionInContestKirche = "2db8164f-ae7a-40d2-a5e3-5aba48bcc70a";
    public const string ElectionGroupIdGossauMajorityElectionInContestStGallen = "08f15cd4-63af-408d-8238-661b15e64409";

    public const string BallotGroupIdStGallenMajorityElectionInContestBund = "7a32239e-1cd1-4deb-9ecb-f5f2aa2f9949";
    public const string BallotGroupId1GossauMajorityElectionInContestBund = "d432ee79-92d4-4494-8dc8-04686bdcb30b";
    public const string BallotGroupId2GossauMajorityElectionInContestBund = "754d6ea4-9e9f-4861-b9ad-62ee2bffc508";
    public const string BallotGroupIdUzwilMajorityElectionInContestStGallen = "0034f358-1baa-47b4-a669-e648b5493f1e";
    public const string BallotGroupIdKircheMajorityElectionInContestKirche = "722a3a08-dede-4366-be30-fcb7c08cc010";

    public const string BallotGroupEntryId1StGallenMajorityElectionInContestBund = "513f5663-13f1-463b-b24d-4b3a7e2f7446";
    public const string BallotGroupEntryId2StGallenMajorityElectionInContestBund = "b0a01fbd-ea47-4228-91d4-8f3054d5fe93";
    public const string BallotGroupEntryId11GossauMajorityElectionInContestBund = "ef3140e2-9105-4c90-b0bd-4d9410bba36c";
    public const string BallotGroupEntryId12GossauMajorityElectionInContestBund = "fbf84e2d-c042-4ec6-9aea-7604d294b828";
    public const string BallotGroupEntryId21GossauMajorityElectionInContestBund = "7b620e4d-ec6e-4302-b6ab-bf2e4b3a0084";
    public const string BallotGroupEntryId22GossauMajorityElectionInContestBund = "8b24be16-7035-4831-9ced-12ed98dff06d";

    public static MajorityElection BundMajorityElectionInContestBund
        => new MajorityElection
        {
            Id = Guid.Parse(IdBundMajorityElectionInContestBund),
            PoliticalBusinessNumber = "100",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl Bund"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl Bund"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidBund,
            ContestId = ContestMockedData.BundContest.Id,
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 0,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ResultEntry = MajorityElectionResultEntry.Detailed,
            EnforceResultEntryForCountingCircles = true,
            CandidateCheckDigit = true,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.AbsoluteMajority,
            NumberOfMandates = 5,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
            MajorityElectionCandidates = new List<MajorityElectionCandidate>
            {
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateIdBundMajorityElectionInContestBund),
                        FirstName = "firstName",
                        LastName = "lastName",
                        PoliticalFirstName = "pol first name",
                        PoliticalLastName = "pol last name",
                        Occupation = LanguageUtil.MockAllLanguages("occupation"),
                        OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                        DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = true,
                        Position = 1,
                        Party = LanguageUtil.MockAllLanguages("CVP"),
                        Locality = "locality",
                        Number = "number1",
                        Sex = SexType.Female,
                        Title = "title",
                        ZipCode = "zip code",
                        Origin = "origin",
                        CheckDigit = 9,
                    },
            },
        };

    public static MajorityElection StGallenMajorityElectionInContestBund
        => new MajorityElection
        {
            Id = Guid.Parse(IdStGallenMajorityElectionInContestBund),
            PoliticalBusinessNumber = "201",
            OfficialDescription = LanguageUtil.MockAllLanguages("Majorzwahl St. Gallen"),
            ShortDescription = LanguageUtil.MockAllLanguages("Majorzwahl SG"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidStGallen,
            ContestId = ContestMockedData.BundContest.Id,
            Active = true,
            AutomaticEmptyVoteCounting = false,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 3,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ResultEntry = MajorityElectionResultEntry.Detailed,
            EnforceResultEntryForCountingCircles = false,
            CandidateCheckDigit = false,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.RelativeMajority,
            NumberOfMandates = 1,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = false,
            MajorityElectionCandidates = new List<MajorityElectionCandidate>
            {
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateId1StGallenMajorityElectionInContestBund),
                        FirstName = "firstName",
                        LastName = "lastName",
                        PoliticalFirstName = "pol first name",
                        PoliticalLastName = "pol last name",
                        Occupation = LanguageUtil.MockAllLanguages("occupation"),
                        OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                        DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = true,
                        Position = 1,
                        Party = LanguageUtil.MockAllLanguages("Test"),
                        Locality = "locality",
                        Number = "number1",
                        Sex = SexType.Female,
                        Title = "title",
                        ZipCode = "zip code",
                        Origin = "origin",
                        CheckDigit = 9,
                    },
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateId2StGallenMajorityElectionInContestBund),
                        FirstName = "firstName2",
                        LastName = "lastName2",
                        PoliticalFirstName = "pol first name",
                        PoliticalLastName = "pol last name",
                        Occupation = LanguageUtil.MockAllLanguages("occupation2"),
                        OccupationTitle = LanguageUtil.MockAllLanguages("occupation title2"),
                        DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = false,
                        Position = 2,
                        Party = LanguageUtil.MockAllLanguages("Test"),
                        Locality = "locality",
                        Number = "number5",
                        Sex = SexType.Male,
                        Title = "title",
                        ZipCode = "zip code",
                        Origin = "origin",
                        CheckDigit = 1,
                    },
            },
            SecondaryMajorityElections = new List<SecondaryMajorityElection>
            {
                    new SecondaryMajorityElection
                    {
                        Id = Guid.Parse(SecondaryElectionIdStGallenMajorityElectionInContestBund),
                        OfficialDescription = LanguageUtil.MockAllLanguages("Nebenwahl St. Gallen"),
                        ShortDescription = LanguageUtil.MockAllLanguages("Nebenwahl SG"),
                        NumberOfMandates = 3,
                        PoliticalBusinessNumber = "n1",
                        AllowedCandidates = SecondaryMajorityElectionAllowedCandidate.MayExistInPrimaryElection,
                        ElectionGroupId = Guid.Parse(ElectionGroupIdStGallenMajorityElectionInContestBund),
                        Active = false,
                        Candidates = new List<SecondaryMajorityElectionCandidate>
                        {
                            new SecondaryMajorityElectionCandidate
                            {
                                Id = Guid.Parse(SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund),
                                FirstName = "firstName",
                                LastName = "lastName",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                Occupation = LanguageUtil.MockAllLanguages("occupation"),
                                OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Party = LanguageUtil.MockAllLanguages("Test"),
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                CandidateReferenceId = Guid.Parse(CandidateId1StGallenMajorityElectionInContestBund),
                                Origin = "origin",
                                CheckDigit = 9,
                            },
                            new SecondaryMajorityElectionCandidate
                            {
                                Id = Guid.Parse(SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund),
                                FirstName = "Peter",
                                LastName = "Lustig",
                                PoliticalFirstName = "Pete",
                                PoliticalLastName = "L",
                                Occupation = LanguageUtil.MockAllLanguages("Beruf"),
                                OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                                DateOfBirth = new DateTime(1982, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = false,
                                Position = 2,
                                Party = LanguageUtil.MockAllLanguages("CVP"),
                                Locality = "locality",
                                Number = "number2",
                                Sex = SexType.Male,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                CheckDigit = 7,
                            },
                        },
                    },
            },
            ElectionGroup = new ElectionGroup
            {
                Id = Guid.Parse(ElectionGroupIdStGallenMajorityElectionInContestBund),
                Description = "Test Election Group",
                Number = 1,
            },
            BallotGroups = new List<MajorityElectionBallotGroup>
            {
                    new MajorityElectionBallotGroup
                    {
                        Id = Guid.Parse(BallotGroupIdStGallenMajorityElectionInContestBund),
                        Description = "BG1 long description",
                        ShortDescription = "BG1",
                        Position = 1,
                        Entries = new List<MajorityElectionBallotGroupEntry>
                        {
                            new MajorityElectionBallotGroupEntry
                            {
                                Id = Guid.Parse(BallotGroupEntryId1StGallenMajorityElectionInContestBund),
                                PrimaryMajorityElectionId = Guid.Parse(IdStGallenMajorityElectionInContestBund),
                                BlankRowCount = 0,
                                Candidates = new List<MajorityElectionBallotGroupEntryCandidate>
                                {
                                    new MajorityElectionBallotGroupEntryCandidate
                                    {
                                        Id = Guid.Parse("5ce4ebba-554e-4e8d-b603-b36155d11af5"),
                                        PrimaryElectionCandidateId = Guid.Parse(CandidateId1StGallenMajorityElectionInContestBund),
                                    },
                                },
                            },
                            new MajorityElectionBallotGroupEntry
                            {
                                Id = Guid.Parse(BallotGroupEntryId2StGallenMajorityElectionInContestBund),
                                SecondaryMajorityElectionId = Guid.Parse(SecondaryElectionIdStGallenMajorityElectionInContestBund),
                                BlankRowCount = 0,
                                Candidates = new List<MajorityElectionBallotGroupEntryCandidate>
                                {
                                    new MajorityElectionBallotGroupEntryCandidate
                                    {
                                        Id = Guid.Parse("283164ab-eb50-4035-8602-9d49d1ff1a51"),
                                        SecondaryElectionCandidateId = Guid.Parse(SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund),
                                    },
                                },
                            },
                        },
                    },
            },
        };

    public static MajorityElection BundMajorityElectionInContestStGallen
        => new MajorityElection
        {
            Id = Guid.Parse(IdBundMajorityElectionInContestStGallen),
            PoliticalBusinessNumber = "100",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl Bund"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl Bund"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidBund,
            ContestId = ContestMockedData.StGallenEvotingContest.Id,
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 0,
            BallotBundleSampleSize = 0,
            AutomaticBallotBundleNumberGeneration = false,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ResultEntry = MajorityElectionResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = true,
            CandidateCheckDigit = true,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.AbsoluteMajority,
            NumberOfMandates = 5,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
            MajorityElectionCandidates = new List<MajorityElectionCandidate>
            {
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateId1BundMajorityElectionInContestStGallen),
                        FirstName = "firstName",
                        LastName = "lastName",
                        PoliticalFirstName = "pol first name",
                        PoliticalLastName = "pol last name",
                        Occupation = LanguageUtil.MockAllLanguages("occupation"),
                        OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                        DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = true,
                        Position = 1,
                        Party = LanguageUtil.MockAllLanguages("SP"),
                        Locality = "locality",
                        Number = "number1",
                        Sex = SexType.Female,
                        Title = "title",
                        ZipCode = "zip code",
                        Origin = "origin",
                        CheckDigit = 9,
                    },
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateId2BundMajorityElectionInContestStGallen),
                        FirstName = "firstName 2",
                        LastName = "lastName 2",
                        PoliticalFirstName = "pol first name 2",
                        PoliticalLastName = "pol last name 2",
                        Occupation = LanguageUtil.MockAllLanguages("occupation 2"),
                        OccupationTitle = LanguageUtil.MockAllLanguages("occupation title 2"),
                        DateOfBirth = new DateTime(1980, 3, 27, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = false,
                        Position = 2,
                        Party = LanguageUtil.MockAllLanguages("SVP"),
                        Locality = "locality 2",
                        Number = "number2",
                        Sex = SexType.Female,
                        Title = "title2",
                        ZipCode = "zip code2",
                        Origin = "origin 2",
                        CheckDigit = 7,
                    },
            },
        };

    public static MajorityElection UzwilMajorityElectionInContestStGallen
        => new MajorityElection
        {
            Id = Guid.Parse(IdUzwilMajorityElectionInContestStGallen),
            PoliticalBusinessNumber = "166",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl Uzwil"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl Uzwil"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidUzwil,
            ContestId = ContestMockedData.StGallenEvotingContest.Id,
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = false,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 5,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.ContinuousForAllBundles,
            ResultEntry = MajorityElectionResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = true,
            CandidateCheckDigit = true,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.RelativeMajority,
            NumberOfMandates = 5,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
            MajorityElectionCandidates = new List<MajorityElectionCandidate>
            {
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateIdUzwilMajorityElectionInContestStGallen),
                        FirstName = "firstName",
                        LastName = "lastName",
                        PoliticalFirstName = "pol first name",
                        PoliticalLastName = "pol last name",
                        Occupation = LanguageUtil.MockAllLanguages("occupation"),
                        OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                        DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = true,
                        Position = 1,
                        Party = LanguageUtil.MockAllLanguages("FDP"),
                        Locality = "locality",
                        Number = "number1",
                        Sex = SexType.Female,
                        Title = "title",
                        ZipCode = "zip code",
                        Origin = "origin",
                        CheckDigit = 9,
                    },
            },
            SecondaryMajorityElections = new List<SecondaryMajorityElection>
            {
                    new SecondaryMajorityElection
                    {
                        Id = Guid.Parse(SecondaryElectionIdUzwilMajorityElectionInContestStGallen),
                        ShortDescription = LanguageUtil.MockAllLanguages("short"),
                        OfficialDescription = LanguageUtil.MockAllLanguages("official"),
                        NumberOfMandates = 2,
                        PoliticalBusinessNumber = "n1",
                        AllowedCandidates = SecondaryMajorityElectionAllowedCandidate.MustExistInPrimaryElection,
                        ElectionGroupId = Guid.Parse(ElectionGroupIdUzwilMajorityElectionInContestStGallen),
                        Active = false,
                        Candidates = new List<SecondaryMajorityElectionCandidate>
                        {
                            new SecondaryMajorityElectionCandidate
                            {
                                Id = Guid.Parse(SecondaryElectionCandidateIdUzwilMajorityElectionInContestStGallen),
                                FirstName = "first",
                                LastName = "last",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                Occupation = LanguageUtil.MockAllLanguages("occupation"),
                                OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                                DateOfBirth = new DateTime(1960, 2, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Party = LanguageUtil.MockAllLanguages("Test"),
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Male,
                                Title = "title",
                                ZipCode = "zip code",
                                CandidateReferenceId = Guid.Parse(CandidateIdUzwilMajorityElectionInContestStGallen),
                                Origin = "origin",
                                CheckDigit = 9,
                            },
                        },
                    },
            },
            ElectionGroup = new ElectionGroup
            {
                Id = Guid.Parse(ElectionGroupIdUzwilMajorityElectionInContestStGallen),
                Description = "Test Election Group",
                Number = 1,
            },
            BallotGroups = new List<MajorityElectionBallotGroup>
            {
                    new MajorityElectionBallotGroup
                    {
                        Id = Guid.Parse(BallotGroupIdUzwilMajorityElectionInContestStGallen),
                        Description = "BG1 long description",
                        ShortDescription = "BG1",
                        Position = 1,
                        Entries = new List<MajorityElectionBallotGroupEntry>
                        {
                            new MajorityElectionBallotGroupEntry
                            {
                                Id = Guid.Parse("73d86ebb-4732-487b-bc4b-7b8a82e08ddb"),
                                PrimaryMajorityElectionId = Guid.Parse(IdUzwilMajorityElectionInContestStGallen),
                                BlankRowCount = 1,
                                Candidates = new List<MajorityElectionBallotGroupEntryCandidate>
                                {
                                    new MajorityElectionBallotGroupEntryCandidate
                                    {
                                        Id = Guid.Parse("e4a4adea-e9e8-45a6-bb16-5015934d87e7"),
                                        PrimaryElectionCandidateId = Guid.Parse(CandidateIdUzwilMajorityElectionInContestStGallen),
                                    },
                                },
                                IndividualCandidatesVoteCount = 1,
                            },
                            new MajorityElectionBallotGroupEntry
                            {
                                Id = Guid.Parse("91e4d730-41a7-4b8c-ad7f-2647c03af8c8"),
                                SecondaryMajorityElectionId = Guid.Parse(SecondaryElectionIdUzwilMajorityElectionInContestStGallen),
                                BlankRowCount = 0,
                                Candidates = new List<MajorityElectionBallotGroupEntryCandidate>
                                {
                                    new MajorityElectionBallotGroupEntryCandidate
                                    {
                                        Id = Guid.Parse("53e7c8f3-330f-4d5e-847e-046ee1e89372"),
                                        SecondaryElectionCandidateId = Guid.Parse(SecondaryElectionCandidateIdUzwilMajorityElectionInContestStGallen),
                                    },
                                },
                                IndividualCandidatesVoteCount = 1,
                            },
                        },
                    },
            },
        };

    public static MajorityElection StGallenMajorityElectionInContestStGallen
        => new MajorityElection
        {
            Id = Guid.Parse(IdStGallenMajorityElectionInContestStGallen),
            PoliticalBusinessNumber = "155",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl St.Gallen"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl St.Gallen"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidStGallen,
            ContestId = ContestMockedData.StGallenEvotingContest.Id,
            Active = true,
            AutomaticEmptyVoteCounting = false,
            EnforceEmptyVoteCountingForCountingCircles = false,
            BallotBundleSize = 50,
            BallotBundleSampleSize = 1,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.ContinuousForAllBundles,
            ResultEntry = MajorityElectionResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = true,
            CandidateCheckDigit = false,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.RelativeMajority,
            NumberOfMandates = 5,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
            MajorityElectionCandidates = new List<MajorityElectionCandidate>
            {
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateIdStGallenMajorityElectionInContestStGallen),
                        FirstName = "firstName",
                        LastName = "lastName",
                        PoliticalFirstName = "pol first name",
                        PoliticalLastName = "pol last name",
                        Occupation = LanguageUtil.MockAllLanguages("occupation"),
                        OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                        DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = true,
                        Position = 1,
                        Party = LanguageUtil.MockAllLanguages("GLP"),
                        Locality = "locality",
                        Number = "number1",
                        Sex = SexType.Female,
                        Title = "title",
                        ZipCode = "zip code",
                        Origin = "origin",
                        CheckDigit = 9,
                    },
            },
        };

    public static MajorityElection GossauMajorityElectionInContestStGallen
        => new MajorityElection
        {
            Id = Guid.Parse(IdGossauMajorityElectionInContestStGallen),
            PoliticalBusinessNumber = "321",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl Gossau"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl Gossau"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGossau,
            ContestId = ContestMockedData.StGallenEvotingContest.Id,
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 10,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ResultEntry = MajorityElectionResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = false,
            CandidateCheckDigit = true,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.AbsoluteMajority,
            NumberOfMandates = 3,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = false,
            MajorityElectionCandidates = new List<MajorityElectionCandidate>
            {
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateId1GossauMajorityElectionInContestStGallen),
                        FirstName = "firstName",
                        LastName = "lastName",
                        PoliticalFirstName = "pol first name",
                        PoliticalLastName = "pol last name",
                        Occupation = LanguageUtil.MockAllLanguages("occupation"),
                        OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                        DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = true,
                        Position = 1,
                        Party = LanguageUtil.MockAllLanguages("CVP"),
                        Locality = "locality",
                        Number = "number1",
                        Sex = SexType.Female,
                        Title = "title",
                        ZipCode = "zip code",
                        Origin = "origin",
                        CheckDigit = 9,
                    },
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateId2GossauMajorityElectionInContestStGallen),
                        FirstName = "candidate",
                        LastName = "number 2",
                        PoliticalFirstName = "pol first name 2",
                        PoliticalLastName = "pol last name 2",
                        Occupation = LanguageUtil.MockAllLanguages("occupation 2"),
                        OccupationTitle = LanguageUtil.MockAllLanguages("occupation title 2"),
                        DateOfBirth = new DateTime(1940, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = false,
                        Position = 2,
                        Party = LanguageUtil.MockAllLanguages("CVP"),
                        Locality = "locality 2",
                        Number = "number2",
                        Sex = SexType.Female,
                        Title = "title 2",
                        Origin = "origin 2",
                        CheckDigit = 7,
                    },
            },
            SecondaryMajorityElections = new List<SecondaryMajorityElection>
            {
                    new SecondaryMajorityElection
                    {
                        Id = Guid.Parse(SecondaryElectionIdGossauMajorityElectionInContestStGallen),
                        ShortDescription = LanguageUtil.MockAllLanguages("short"),
                        OfficialDescription = LanguageUtil.MockAllLanguages("official"),
                        NumberOfMandates = 3,
                        PoliticalBusinessNumber = "n1",
                        AllowedCandidates = SecondaryMajorityElectionAllowedCandidate.MayExistInPrimaryElection,
                        ElectionGroupId = Guid.Parse(ElectionGroupIdGossauMajorityElectionInContestStGallen),
                        Active = false,
                        Candidates = new List<SecondaryMajorityElectionCandidate>
                        {
                            new SecondaryMajorityElectionCandidate
                            {
                                Id = Guid.Parse(SecondaryElectionCandidateId1GossauMajorityElectionInContestStGallen),
                                FirstName = "secondaryElection firstName with CandidateRef",
                                LastName = "secondaryElection lastName with CandidateRef",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                Occupation = LanguageUtil.MockAllLanguages("occupation"),
                                OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Party = LanguageUtil.MockAllLanguages("Test"),
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                CandidateReferenceId = Guid.Parse(CandidateId1GossauMajorityElectionInContestStGallen),
                                Origin = "origin",
                                CheckDigit = 9,
                            },
                            new SecondaryMajorityElectionCandidate
                            {
                                Id = Guid.Parse(SecondaryElectionCandidateId2GossauMajorityElectionInContestStGallen),
                                FirstName = "secondaryElection Peter",
                                LastName = "secondaryElection Lustig",
                                PoliticalFirstName = "Pete",
                                PoliticalLastName = "L",
                                Occupation = LanguageUtil.MockAllLanguages("Beruf"),
                                OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                                DateOfBirth = new DateTime(1982, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = false,
                                Position = 2,
                                Party = LanguageUtil.MockAllLanguages("CVP"),
                                Locality = "locality",
                                Number = "number2",
                                Sex = SexType.Male,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                CheckDigit = 7,
                            },
                        },
                    },
            },
            ElectionGroup = new ElectionGroup
            {
                Id = Guid.Parse(ElectionGroupIdGossauMajorityElectionInContestStGallen),
                Description = "Test Election Group",
                Number = 1,
            },
        };

    public static MajorityElection GossauMajorityElectionInContestBund
        => new MajorityElection
        {
            Id = Guid.Parse(IdGossauMajorityElectionInContestBund),
            PoliticalBusinessNumber = "291",
            OfficialDescription = LanguageUtil.MockAllLanguages("Majorzwahl Gossau"),
            ShortDescription = LanguageUtil.MockAllLanguages("Majorzwahl Gossau"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGossau,
            ContestId = ContestMockedData.BundContest.Id,
            Active = true,
            AutomaticEmptyVoteCounting = false,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 3,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ResultEntry = MajorityElectionResultEntry.Detailed,
            EnforceResultEntryForCountingCircles = false,
            CandidateCheckDigit = false,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.RelativeMajority,
            NumberOfMandates = 1,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = false,
            MajorityElectionCandidates = new List<MajorityElectionCandidate>
            {
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateId1GossauMajorityElectionInContestBund),
                        FirstName = "firstName",
                        LastName = "lastName",
                        PoliticalFirstName = "pol first name",
                        PoliticalLastName = "pol last name",
                        Occupation = LanguageUtil.MockAllLanguages("occupation"),
                        OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                        DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = true,
                        Position = 1,
                        Party = LanguageUtil.MockAllLanguages("Test"),
                        Locality = "locality",
                        Number = "number1",
                        Sex = SexType.Female,
                        Title = "title",
                        ZipCode = "zip code",
                        Origin = "origin",
                        CheckDigit = 9,
                    },
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateId2GossauMajorityElectionInContestBund),
                        FirstName = "firstName2",
                        LastName = "lastName2",
                        PoliticalFirstName = "pol first name",
                        PoliticalLastName = "pol last name",
                        Occupation = LanguageUtil.MockAllLanguages("occupation2"),
                        OccupationTitle = LanguageUtil.MockAllLanguages("occupation title2"),
                        DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = false,
                        Position = 2,
                        Party = LanguageUtil.MockAllLanguages("Test"),
                        Locality = "locality",
                        Number = "number5",
                        Sex = SexType.Male,
                        Title = "title",
                        ZipCode = "zip code",
                        Origin = "origin",
                        CheckDigit = 1,
                    },
            },
            SecondaryMajorityElections = new List<SecondaryMajorityElection>
            {
                    new SecondaryMajorityElection
                    {
                        Id = Guid.Parse(SecondaryElectionIdGossauMajorityElectionInContestBund),
                        ShortDescription = LanguageUtil.MockAllLanguages("short"),
                        OfficialDescription = LanguageUtil.MockAllLanguages("official"),
                        NumberOfMandates = 3,
                        PoliticalBusinessNumber = "n1",
                        AllowedCandidates = SecondaryMajorityElectionAllowedCandidate.MayExistInPrimaryElection,
                        ElectionGroupId = Guid.Parse(ElectionGroupIdGossauMajorityElectionInContestBund),
                        Active = false,
                        Candidates = new List<SecondaryMajorityElectionCandidate>
                        {
                            new SecondaryMajorityElectionCandidate
                            {
                                Id = Guid.Parse(SecondaryElectionCandidateId1GossauMajorityElectionInContestBund),
                                FirstName = "firstName",
                                LastName = "lastName",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                Occupation = LanguageUtil.MockAllLanguages("occupation"),
                                OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Party = LanguageUtil.MockAllLanguages("Test"),
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                CandidateReferenceId = Guid.Parse(CandidateId1GossauMajorityElectionInContestBund),
                                Origin = "origin",
                                CheckDigit = 9,
                            },
                            new SecondaryMajorityElectionCandidate
                            {
                                Id = Guid.Parse(SecondaryElectionCandidateId2GossauMajorityElectionInContestBund),
                                FirstName = "Peter",
                                LastName = "Lustig",
                                PoliticalFirstName = "Pete",
                                PoliticalLastName = "L",
                                Occupation = LanguageUtil.MockAllLanguages("Beruf"),
                                OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                                DateOfBirth = new DateTime(1982, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = false,
                                Position = 2,
                                Party = LanguageUtil.MockAllLanguages("CVP"),
                                Locality = "locality",
                                Number = "number2",
                                Sex = SexType.Male,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                CheckDigit = 7,
                            },
                        },
                    },
            },
            ElectionGroup = new ElectionGroup
            {
                Id = Guid.Parse(ElectionGroupIdGossauMajorityElectionInContestBund),
                Description = "Test Election Group",
                Number = 1,
            },
            BallotGroups = new List<MajorityElectionBallotGroup>
            {
                    new MajorityElectionBallotGroup
                    {
                        Id = Guid.Parse(BallotGroupId1GossauMajorityElectionInContestBund),
                        Description = "BG1 long description",
                        ShortDescription = "BG1",
                        Position = 1,
                        Entries = new List<MajorityElectionBallotGroupEntry>
                        {
                            new MajorityElectionBallotGroupEntry
                            {
                                Id = Guid.Parse(BallotGroupEntryId11GossauMajorityElectionInContestBund),
                                PrimaryMajorityElectionId = Guid.Parse(IdGossauMajorityElectionInContestBund),
                                BlankRowCount = 0,
                                Candidates = new List<MajorityElectionBallotGroupEntryCandidate>
                                {
                                    new MajorityElectionBallotGroupEntryCandidate
                                    {
                                        Id = Guid.Parse("5fccc7cf-d4bc-432d-92e9-79722a4b3bf9"),
                                        PrimaryElectionCandidateId = Guid.Parse(CandidateId1GossauMajorityElectionInContestBund),
                                    },
                                },
                            },
                            new MajorityElectionBallotGroupEntry
                            {
                                Id = Guid.Parse(BallotGroupEntryId12GossauMajorityElectionInContestBund),
                                SecondaryMajorityElectionId = Guid.Parse(SecondaryElectionIdGossauMajorityElectionInContestBund),
                                BlankRowCount = 1,
                                Candidates = new List<MajorityElectionBallotGroupEntryCandidate>
                                {
                                    new MajorityElectionBallotGroupEntryCandidate
                                    {
                                        Id = Guid.Parse("8b15a05b-9cee-4792-b099-69ba69bae4f7"),
                                        SecondaryElectionCandidateId = Guid.Parse(SecondaryElectionCandidateId1GossauMajorityElectionInContestBund),
                                    },
                                },
                                IndividualCandidatesVoteCount = 1,
                            },
                        },
                    },
                    new MajorityElectionBallotGroup
                    {
                        Id = Guid.Parse(BallotGroupId2GossauMajorityElectionInContestBund),
                        Description = "BG2 long description",
                        ShortDescription = "BG2",
                        Position = 2,
                        Entries = new List<MajorityElectionBallotGroupEntry>
                        {
                            new MajorityElectionBallotGroupEntry
                            {
                                Id = Guid.Parse(BallotGroupEntryId21GossauMajorityElectionInContestBund),
                                PrimaryMajorityElectionId = Guid.Parse(IdGossauMajorityElectionInContestBund),
                                BlankRowCount = 0,
                                Candidates = new List<MajorityElectionBallotGroupEntryCandidate>
                                {
                                    new MajorityElectionBallotGroupEntryCandidate
                                    {
                                        Id = Guid.Parse("fd199ae6-625b-4fcf-92b7-9cd23ad254c9"),
                                        PrimaryElectionCandidateId = Guid.Parse(CandidateId1GossauMajorityElectionInContestBund),
                                    },
                                },
                            },
                            new MajorityElectionBallotGroupEntry
                            {
                                Id = Guid.Parse(BallotGroupEntryId22GossauMajorityElectionInContestBund),
                                SecondaryMajorityElectionId = Guid.Parse(SecondaryElectionIdGossauMajorityElectionInContestBund),
                                BlankRowCount = 0,
                                Candidates = new List<MajorityElectionBallotGroupEntryCandidate>
                                {
                                    new MajorityElectionBallotGroupEntryCandidate
                                    {
                                        Id = Guid.Parse("77a31b50-3c24-49a2-94c2-9ee2900f3a13"),
                                        SecondaryElectionCandidateId = Guid.Parse(SecondaryElectionCandidateId2GossauMajorityElectionInContestBund),
                                    },
                                },
                            },
                        },
                    },
            },
        };

    public static MajorityElection StGallenMajorityElectionInContestStGallenWithoutChilds
        => new MajorityElection
        {
            Id = Guid.Parse(IdStGallenMajorityElectionInContestStGallenWithoutChilds),
            PoliticalBusinessNumber = "500",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl St.Gallen 2"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl St.Gallen 2"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidStGallen,
            ContestId = ContestMockedData.StGallenEvotingContest.Id,
            Active = false,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ResultEntry = MajorityElectionResultEntry.Detailed,
            EnforceResultEntryForCountingCircles = true,
            CandidateCheckDigit = true,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.AbsoluteMajority,
            NumberOfMandates = 5,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = false,
        };

    public static MajorityElection GossauMajorityElectionInContestGossau
        => new MajorityElection
        {
            Id = Guid.Parse(IdGossauMajorityElectionInContestGossau),
            PoliticalBusinessNumber = "324",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl Gossau"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl Gossau"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGossau,
            ContestId = ContestMockedData.GossauContest.Id,
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 20,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ResultEntry = MajorityElectionResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = false,
            CandidateCheckDigit = true,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.AbsoluteMajority,
            NumberOfMandates = 5,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
            MajorityElectionCandidates = new List<MajorityElectionCandidate>
            {
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateIdGossauMajorityElectionInContestGossau),
                        FirstName = "firstName",
                        LastName = "lastName",
                        PoliticalFirstName = "pol first name",
                        PoliticalLastName = "pol last name",
                        Occupation = LanguageUtil.MockAllLanguages("occupation"),
                        OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                        DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = true,
                        Position = 1,
                        Party = LanguageUtil.MockAllLanguages("Test"),
                        Locality = "locality",
                        Number = "number1",
                        Sex = SexType.Female,
                        Title = "title",
                        ZipCode = "zip code",
                        Origin = "origin",
                        CheckDigit = 9,
                    },
            },
        };

    public static MajorityElection UzwilMajorityElectionInContestUzwil
        => new MajorityElection
        {
            Id = Guid.Parse(IdUzwilMajorityElectionInContestUzwilWithoutChilds),
            PoliticalBusinessNumber = "412",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl Uzwil"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl Uzwil"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidUzwil,
            ContestId = ContestMockedData.UzwilEvotingContest.Id,
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 25,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ResultEntry = MajorityElectionResultEntry.Detailed,
            EnforceResultEntryForCountingCircles = false,
            CandidateCheckDigit = true,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.AbsoluteMajority,
            NumberOfMandates = 5,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
            MajorityElectionCandidates = new List<MajorityElectionCandidate>
            {
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateIdUzwilMajorityElectionInContestUzwil),
                        FirstName = "firstName",
                        LastName = "lastName",
                        PoliticalFirstName = "pol first name",
                        PoliticalLastName = "pol last name",
                        Occupation = LanguageUtil.MockAllLanguages("occupation"),
                        OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                        DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = true,
                        Position = 1,
                        Party = LanguageUtil.MockAllLanguages("None"),
                        Locality = "locality",
                        Number = "number1",
                        Sex = SexType.Female,
                        Title = "title",
                        ZipCode = "zip code",
                        Origin = "origin",
                        CheckDigit = 9,
                    },
            },
        };

    public static MajorityElection UzwilMajorityElectionInContestBundWithoutChilds
        => new MajorityElection
        {
            Id = Guid.Parse(IdUzwilMajorityElectionInContestBund),
            PoliticalBusinessNumber = "714",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl Uzwil"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl Uzwil"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidUzwil,
            Active = false,
            ContestId = ContestMockedData.BundContest.Id,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 2,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ResultEntry = MajorityElectionResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = false,
            CandidateCheckDigit = true,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.AbsoluteMajority,
            NumberOfMandates = 5,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
        };

    public static MajorityElection GenfMajorityElectionInContestBundWithoutChilds
        => new MajorityElection
        {
            Id = Guid.Parse(IdGenfMajorityElectionInContestBundWithoutChilds),
            PoliticalBusinessNumber = "714a",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl Genf"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl Genf"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGenf,
            ContestId = ContestMockedData.BundContest.Id,
            Active = false,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 1,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ResultEntry = MajorityElectionResultEntry.Detailed,
            EnforceResultEntryForCountingCircles = true,
            CandidateCheckDigit = true,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.AbsoluteMajority,
            NumberOfMandates = 5,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
        };

    public static MajorityElection KircheMajorityElectionInContestKirche
        => new MajorityElection
        {
            Id = Guid.Parse(IdKircheMajorityElectionInContestKirche),
            PoliticalBusinessNumber = "aaa",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl Kirche"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl Kirche"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidKirchgemeinde,
            ContestId = ContestMockedData.KirchenContest.Id,
            Active = false,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ResultEntry = MajorityElectionResultEntry.Detailed,
            EnforceResultEntryForCountingCircles = false,
            CandidateCheckDigit = true,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.AbsoluteMajority,
            NumberOfMandates = 5,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
            MajorityElectionCandidates = new List<MajorityElectionCandidate>
            {
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateIdKircheMajorityElectionInContestKirche),
                        FirstName = "firstName",
                        LastName = "lastName",
                        PoliticalFirstName = "pol first name",
                        PoliticalLastName = "pol last name",
                        Occupation = LanguageUtil.MockAllLanguages("occupation"),
                        OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                        DateOfBirth = new DateTime(1970, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = false,
                        Position = 1,
                        Party = LanguageUtil.MockAllLanguages("test"),
                        Locality = "locality",
                        Number = "number1",
                        Sex = SexType.Male,
                        Title = "title",
                        Origin = "origin",
                        CheckDigit = 9,
                    },
            },
            SecondaryMajorityElections = new List<SecondaryMajorityElection>
            {
                    new SecondaryMajorityElection
                    {
                        Id = Guid.Parse(SecondaryElectionIdKircheMajorityElectionInContestKirche),
                        ShortDescription = LanguageUtil.MockAllLanguages("short"),
                        OfficialDescription = LanguageUtil.MockAllLanguages("official"),
                        NumberOfMandates = 2,
                        PoliticalBusinessNumber = "n1",
                        AllowedCandidates = SecondaryMajorityElectionAllowedCandidate.MustNotExistInPrimaryElection,
                        ElectionGroupId = Guid.Parse(ElectionGroupIdKircheMajorityElectionInContestKirche),
                        Active = false,
                        Candidates = new List<SecondaryMajorityElectionCandidate>
                        {
                            new SecondaryMajorityElectionCandidate
                            {
                                Id = Guid.Parse(SecondaryElectionCandidateIdKircheMajorityElectionInContestKirche),
                                FirstName = "first",
                                LastName = "last",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                Occupation = LanguageUtil.MockAllLanguages("occupation"),
                                OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                                DateOfBirth = new DateTime(1980, 12, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = false,
                                Position = 1,
                                Party = LanguageUtil.MockAllLanguages("Test"),
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Male,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                CheckDigit = 9,
                            },
                        },
                    },
            },
            ElectionGroup = new ElectionGroup
            {
                Id = Guid.Parse(ElectionGroupIdKircheMajorityElectionInContestKirche),
                Description = "Test Election Group",
                Number = 1,
            },
            BallotGroups = new List<MajorityElectionBallotGroup>
            {
                    new MajorityElectionBallotGroup
                    {
                        Id = Guid.Parse(BallotGroupIdKircheMajorityElectionInContestKirche),
                        Description = "BG1 long description",
                        ShortDescription = "BG1",
                        Position = 1,
                        Entries = new List<MajorityElectionBallotGroupEntry>
                        {
                            new MajorityElectionBallotGroupEntry
                            {
                                Id = Guid.Parse("36e30984-9949-452a-b949-d384966680f1"),
                                PrimaryMajorityElectionId = Guid.Parse(IdKircheMajorityElectionInContestKirche),
                                BlankRowCount = 0,
                                Candidates = new List<MajorityElectionBallotGroupEntryCandidate>
                                {
                                    new MajorityElectionBallotGroupEntryCandidate
                                    {
                                        Id = Guid.Parse("16aedf82-87db-4fac-b941-4d5b22d48838"),
                                        PrimaryElectionCandidateId = Guid.Parse(CandidateIdKircheMajorityElectionInContestKirche),
                                    },
                                },
                            },
                            new MajorityElectionBallotGroupEntry
                            {
                                Id = Guid.Parse("5539d962-2cbe-4e5e-ab55-eabf2866aefa"),
                                SecondaryMajorityElectionId = Guid.Parse(SecondaryElectionIdKircheMajorityElectionInContestKirche),
                                BlankRowCount = 0,
                                Candidates = new List<MajorityElectionBallotGroupEntryCandidate>
                                {
                                    new MajorityElectionBallotGroupEntryCandidate
                                    {
                                        Id = Guid.Parse("46cd92ef-1837-42c1-a828-a9df02b1cbb5"),
                                        SecondaryElectionCandidateId = Guid.Parse(SecondaryElectionCandidateIdKircheMajorityElectionInContestKirche),
                                    },
                                },
                            },
                        },
                    },
            },
        };

    public static MajorityElection KircheMajorityElectionInContestKircheWithoutChilds
        => new MajorityElection
        {
            Id = Guid.Parse(IdKircheMajorityElectionInContestKircheWithoutChilds),
            PoliticalBusinessNumber = "aaa",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl Kirche ohne Listen"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl Kirche ohne Listen"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidKirchgemeinde,
            ContestId = ContestMockedData.KirchenContest.Id,
            Active = false,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 10,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ResultEntry = MajorityElectionResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = false,
            CandidateCheckDigit = true,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.AbsoluteMajority,
            NumberOfMandates = 5,
            ReviewProcedure = MajorityElectionReviewProcedure.Physically,
            EnforceReviewProcedureForCountingCircles = false,
            EnforceCandidateCheckDigitForCountingCircles = false,
        };

    public static IEnumerable<MajorityElection> All
    {
        get
        {
            yield return BundMajorityElectionInContestBund;
            yield return BundMajorityElectionInContestStGallen;
            yield return UzwilMajorityElectionInContestStGallen;
            yield return StGallenMajorityElectionInContestBund;
            yield return StGallenMajorityElectionInContestStGallen;
            yield return GossauMajorityElectionInContestStGallen;
            yield return GossauMajorityElectionInContestBund;
            yield return StGallenMajorityElectionInContestStGallenWithoutChilds;
            yield return GossauMajorityElectionInContestGossau;
            yield return UzwilMajorityElectionInContestUzwil;
            yield return UzwilMajorityElectionInContestBundWithoutChilds;
            yield return GenfMajorityElectionInContestBundWithoutChilds;
            yield return KircheMajorityElectionInContestKirche;
            yield return KircheMajorityElectionInContestKircheWithoutChilds;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped, bool seedContestAndUnion = true)
    {
        if (seedContestAndUnion)
        {
            await ContestMockedData.Seed(runScoped);
        }

        await runScoped(async sp =>
        {
            var all = All.ToList();
            var simplePbBuilder = sp.GetRequiredService<SimplePoliticalBusinessBuilder<MajorityElection>>();
            var simpleSecondaryPbBuilder = sp.GetRequiredService<SimplePoliticalBusinessBuilder<SecondaryMajorityElection>>();
            var db = sp.GetRequiredService<DataContext>();
            db.MajorityElections.AddRange(all);
            await db.SaveChangesAsync();

            foreach (var election in all)
            {
                await simplePbBuilder.Create(election);

                foreach (var secondaryElection in election.SecondaryMajorityElections)
                {
                    await simpleSecondaryPbBuilder.Create(secondaryElection);
                }
            }

            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues(string.Empty, "test", "test", Enumerable.Empty<string>());

            var aggregateRepository = sp.GetRequiredService<IAggregateRepository>();
            var aggregateFactory = sp.GetRequiredService<IAggregateFactory>();
            var mapper = sp.GetRequiredService<TestMapper>();

            var aggregates = All.Select(pe => ToAggregate(pe, aggregateFactory, mapper));

            foreach (var proportionalElections in aggregates)
            {
                await aggregateRepository.Save(proportionalElections);
            }

            sp.GetRequiredService<EventPublisherMock>().Clear();
        });

        if (seedContestAndUnion)
        {
            await MajorityElectionUnionMockedData.Seed(runScoped);
        }
    }

    public static MajorityElectionAggregate ToAggregate(
        MajorityElection majorityElection,
        IAggregateFactory aggregateFactory,
        TestMapper mapper)
    {
        var aggregate = aggregateFactory.New<MajorityElectionAggregate>();
        var domainElection = mapper.Map<Core.Domain.MajorityElection>(majorityElection);
        var doi = DomainOfInfluenceMockedData.All.First(x => x.Id == domainElection.DomainOfInfluenceId);

        aggregate.CreateFrom(domainElection);

        foreach (var candidate in majorityElection.MajorityElectionCandidates)
        {
            candidate.MajorityElectionId = aggregate.Id;
            var domainCandidate = mapper.Map<Core.Domain.MajorityElectionCandidate>(candidate);
            aggregate.CreateCandidateFrom(domainCandidate, doi.Type);
        }

        if (majorityElection.ElectionGroup != null)
        {
            var domainElectionGroup = mapper.Map<Core.Domain.ElectionGroup>(majorityElection.ElectionGroup);
            domainElectionGroup.PrimaryMajorityElectionId = majorityElection.Id;
            aggregate.CreateElectionGroupFrom(domainElectionGroup);
        }

        foreach (var secondaryElection in majorityElection.SecondaryMajorityElections)
        {
            var domainSecondaryElection = mapper.Map<Core.Domain.SecondaryMajorityElection>(secondaryElection);
            domainSecondaryElection.PrimaryMajorityElectionId = domainElection.Id;
            aggregate.CreateSecondaryMajorityElectionFrom(domainSecondaryElection);

            foreach (var candidate in secondaryElection.Candidates)
            {
                if (candidate.CandidateReferenceId.HasValue)
                {
                    var domainCandidateReference = new MajorityElectionCandidateReference
                    {
                        Id = candidate.Id,
                        Incumbent = candidate.Incumbent,
                        Position = candidate.Position,
                        SecondaryMajorityElectionId = domainSecondaryElection.Id,
                        CandidateId = candidate.CandidateReferenceId.Value,
                    };
                    aggregate.CreateCandidateReferenceFrom(domainCandidateReference);
                }
                else
                {
                    var domainCandidate = mapper.Map<Core.Domain.MajorityElectionCandidate>(candidate);
                    domainCandidate.MajorityElectionId = domainSecondaryElection.Id;
                    aggregate.CreateSecondaryMajorityElectionCandidateFrom(domainCandidate, doi.Type);
                }
            }
        }

        foreach (var ballotGroup in majorityElection.BallotGroups)
        {
            var domainBallotGroup = mapper.Map<Core.Domain.MajorityElectionBallotGroup>(ballotGroup);
            domainBallotGroup.MajorityElectionId = domainElection.Id;
            aggregate.CreateBallotGroupFrom(domainBallotGroup);

            if (ballotGroup.Entries.Count == 0)
            {
                continue;
            }

            var candidates = new MajorityElectionBallotGroupCandidates { BallotGroupId = domainBallotGroup.Id };
            foreach (var entry in ballotGroup.Entries)
            {
                candidates.EntryCandidates.Add(new MajorityElectionBallotGroupEntryCandidates
                {
                    BallotGroupEntryId = entry.Id,
                    CandidateIds = entry.Candidates.Select(c => c.PrimaryElectionCandidateId ?? c.SecondaryElectionCandidateId!.Value).ToList(),
                    IndividualCandidatesVoteCount = entry.IndividualCandidatesVoteCount,
                });
            }

            aggregate.UpdateBallotGroupCandidates(candidates, false);
        }

        return aggregate;
    }
}
