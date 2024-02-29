// (c) Copyright 2024 by Abraxas Informatik AG
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
using ProportionalElection = Voting.Basis.Data.Models.ProportionalElection;
using ProportionalElectionCandidate = Voting.Basis.Data.Models.ProportionalElectionCandidate;
using ProportionalElectionList = Voting.Basis.Data.Models.ProportionalElectionList;
using ProportionalElectionListUnion = Voting.Basis.Data.Models.ProportionalElectionListUnion;

namespace Voting.Basis.Test.MockedData;

public static class ProportionalElectionMockedData
{
    public const string IdBundProportionalElectionInContestBund = "053d1197-ddb2-4906-8c90-b9baa45a40fb";
    public const string IdStGallenProportionalElectionInContestBund = "3a832f45-34c0-47ce-b1e3-db27b97948ba";
    public const string IdUzwilProportionalElectionInContestBund = "a73b1bf3-7bbe-44fb-9b65-8f5e1734ad72";
    public const string IdBundProportionalElectionInContestStGallen = "30e170ba-ed97-4886-93c9-ee35b106a22e";
    public const string IdGossauProportionalElectionInContestStGallen = "fa69e964-0a02-4d16-b417-247e8987021a";
    public const string IdGossauProportionalElectionInContestBund = "b849600e-305b-4d37-a999-214acdc88682";
    public const string IdUzwilProportionalElectionInContestStGallen = "da091f50-9f11-4deb-9621-948fbfbdc322";
    public const string IdStGallenProportionalElectionInContestStGallen = "8fd00ee5-cc68-4b33-86b0-cc9c58dc1b1f";
    public const string IdStGallenProportionalElectionInContestStGallenWithoutChilds = "06f7ecb7-e175-4c3b-9ea5-cfc138d08278";
    public const string IdGossauProportionalElectionInContestGossau = "81880186-febc-4fd7-bb82-62c446430027";
    public const string IdUzwilProportionalElectionInContestUzwilWithoutChilds = "76d1c7ed-85ec-4e62-a540-ca0a83149d32";
    public const string IdGenfProportionalElectionInContestBundWithoutChilds = "61eeda40-6669-4793-9831-ade34e516365";
    public const string IdKircheProportionalElectionInContestKirche = "62fc5770-ad6a-41bd-9375-008a1dc11939";
    public const string IdKircheProportionalElectionInContestKircheWithoutChilds = "27b52067-dcb9-4701-87cc-54d70bc653f4";

    public const string ListIdBundProportionalElectionInContestBund = "5af18d6d-83b7-40c6-997f-248359817a0d";
    public const string List1IdStGallenProportionalElectionInContestBund = "6fa5262f-bf27-4eb9-81d4-23bb1a49d031";
    public const string List2IdStGallenProportionalElectionInContestBund = "05b72caf-23a9-411d-bac3-7d587666b48a";
    public const string ListIdBundProportionalElectionInContestStGallen = "ead283f5-5b06-4d94-b23a-1ddf8fa9079f";
    public const string ListIdUzwilProportionalElectionInContestStGallen = "66dbbea3-0c99-469f-94c5-4314c32e8eab";
    public const string ListIdStGallenProportionalElectionInContestStGallen = "6eedf849-0ecc-4a02-a43b-99ef4b11d795";
    public const string ListId1GossauProportionalElectionInContestStGallen = "9091a3b6-3785-4adc-a486-f486e686503e";
    public const string ListId2GossauProportionalElectionInContestStGallen = "bfe54c2a-6bdf-41a3-bf11-321203c380d3";
    public const string ListId3GossauProportionalElectionInContestStGallen = "afebb285-599d-415f-89ac-04ebcbc4eaeb";
    public const string ListId1GossauProportionalElectionInContestBund = "afab6f5d-4b4b-4e7c-87c4-6eb32b85163b";
    public const string ListId2GossauProportionalElectionInContestBund = "3f834c4b-eabe-4dad-97b2-03d4fc770bf5";
    public const string ListIdGossauProportionalElectionInContestGossau = "84a0c2dd-9c18-4a64-a08f-d2478c0d3a5b";
    public const string ListIdUzwilProportionalElectionInContestUzwil = "3808a9cc-c523-40d8-b341-230d801be63b";
    public const string ListIdKircheProportionalElectionInContestKirche = "3561571f-7b4c-469c-9e1b-65166e8f00f0";

    public const string ListUnion1IdGossauProportionalElectionInContestStGallen = "16892ba3-9b8c-42c7-914e-4b4692d170f4";
    public const string ListUnion2IdGossauProportionalElectionInContestStGallen = "6a8913a3-bd03-4cb3-a0f9-317db5de8959";
    public const string ListUnion3IdGossauProportionalElectionInContestStGallen = "687970fd-aeae-48d0-b291-7b6333912907";
    public const string SubListUnion11IdGossauProportionalElectionInContestStGallen = "5f53066c-1922-497d-a48b-cfd69579d892";
    public const string SubListUnion12IdGossauProportionalElectionInContestStGallen = "7fd14367-ff96-4ddc-89bc-47fb658527df";
    public const string SubListUnion21IdGossauProportionalElectionInContestStGallen = "49715fbf-5399-4981-bee5-01705469ec8c";
    public const string SubListUnion22IdGossauProportionalElectionInContestStGallen = "6a839c1a-c94a-4b5a-b59b-4c0edea82307";
    public const string ListUnionIdGossauProportionalElectionInContestBund = "9a71d553-343c-4b90-8fa7-924f662929d6";
    public const string SubListUnionIdGossauProportionalElectionInContestBund = "93cd8201-f82e-4142-b57d-980e4b80d1a9";
    public const string ListUnionIdStGallenProportionalElectionInContestBund = "007ff21f-e61a-48f0-ab1f-6b3aa2c04c53";
    public const string ListUnionIdBundProportionalElectionInContestStGallen = "c0938e89-e5a4-4ee9-bd78-4ca972ddd68e";
    public const string ListUnionIdUzwilProportionalElectionInContestStGallen = "9d5cb38e-0a75-445f-970d-f97ae129f054";
    public const string ListUnionIdKircheProportionalElectionInContestKirche = "9cb1bb16-d284-427b-841c-6e04cea35b2d";
    public const string ListUnionIdGossauProportionalElectionInContestGossau = "0a8f4968-5546-4198-8c2f-b98b154fd0c6";

    public const string CandidateIdBundProportionalElectionInContestBund = "8ad43b77-2ef2-4241-bd66-8d87de236a74";
    public const string CandidateIdStGallenProportionalElectionInContestBund = "bba39596-a5f6-4729-a56f-e63871b30acc";
    public const string CandidateId1BundProportionalElectionInContestStGallen = "a31bf965-4824-4a05-a4fe-a43a7605b1f8";
    public const string CandidateId2BundProportionalElectionInContestStGallen = "7eaa113f-4273-4a06-b6b2-ee65919249d6";
    public const string CandidateIdUzwilProportionalElectionInContestStGallen = "d009d110-6269-4b6e-b9d1-84508de08d42";
    public const string CandidateIdStGallenProportionalElectionInContestStGallen = "9e131f21-4483-4375-b014-484c272615ee";
    public const string CandidateId1GossauProportionalElectionInContestStGallen = "8b4837a9-c3ba-4ec5-9e50-536a9b4347a9";
    public const string CandidateId2GossauDeletedPartyProportionalElectionInContestStGallen = "9efe090f-883b-4e86-89c2-cd132ea84cbd";
    public const string CandidateId1GossauProportionalElectionInContestBund = "7c13d762-4166-41f6-b3a5-2c5f21c9dc43";
    public const string CandidateId2GossauProportionalElectionInContestBund = "21f29d48-349d-48e4-862f-723f5929db52";
    public const string CandidateIdGossauProportionalElectionInContestGossau = "dd49aaba-ab8d-4eda-b83b-54beb8222af0";
    public const string CandidateIdUzwilProportionalElectionInContestUzwil = "7ddfff64-f55a-41ec-8ff5-0fde639a76c0";
    public const string CandidateIdKircheProportionalElectionInContestKirche = "e2000614-8633-4e35-b667-0eae6edc77e4";

    public static ProportionalElection BundProportionalElectionInContestBund
        => new ProportionalElection
        {
            Id = Guid.Parse(IdBundProportionalElectionInContestBund),
            PoliticalBusinessNumber = "100",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl Bund"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl Bund"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidBund,
            ContestId = ContestMockedData.BundContest.Id,
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 3,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = true,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            NumberOfMandates = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
            ProportionalElectionLists = new List<ProportionalElectionList>
            {
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(ListIdBundProportionalElectionInContestBund),
                        Position = 1,
                        BlankRowCount = 0,
                        OrderNumber = "1a",
                        Description = LanguageUtil.MockAllLanguages("Liste 1 der Partei SVP"),
                        ShortDescription = LanguageUtil.MockAllLanguages("Liste 1"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateIdBundProportionalElectionInContestBund),
                                FirstName = "firstName",
                                LastName = "lastName",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                Occupation = LanguageUtil.MockAllLanguages("occupation"),
                                OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Accumulated = true,
                                AccumulatedPosition = 2,
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                PartyId = DomainOfInfluenceMockedData.GuidPartyBundAndere,
                                Origin = "origin",
                                CheckDigit = 6,
                            },
                        },
                    },
            },
        };

    public static ProportionalElection StGallenProportionalElectionInContestBund
        => new ProportionalElection
        {
            Id = Guid.Parse(IdStGallenProportionalElectionInContestBund),
            PoliticalBusinessNumber = "201",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl St. Gallen"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl SG"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidStGallen,
            ContestId = ContestMockedData.BundContest.Id,
            Active = true,
            AutomaticEmptyVoteCounting = false,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 3,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = false,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.HagenbachBischoff,
            NumberOfMandates = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = false,
            ProportionalElectionLists = new List<ProportionalElectionList>
            {
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(List1IdStGallenProportionalElectionInContestBund),
                        Position = 1,
                        BlankRowCount = 0,
                        OrderNumber = "1a",
                        Description = LanguageUtil.MockAllLanguages("Liste 1"),
                        ShortDescription = LanguageUtil.MockAllLanguages("L1"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateIdStGallenProportionalElectionInContestBund),
                                FirstName = "firstName",
                                LastName = "lastName",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                Occupation = LanguageUtil.MockAllLanguages("occupation"),
                                OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Accumulated = true,
                                AccumulatedPosition = 2,
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                PartyId = DomainOfInfluenceMockedData.GuidPartyStGallenSP,
                                Origin = "origin",
                                CheckDigit = 6,
                            },
                        },
                    },
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(List2IdStGallenProportionalElectionInContestBund),
                        Position = 2,
                        BlankRowCount = 1,
                        OrderNumber = "2a",
                        Description = LanguageUtil.MockAllLanguages("Liste 2"),
                        ShortDescription = LanguageUtil.MockAllLanguages("L2"),
                    },
            },
            ProportionalElectionListUnions = new List<ProportionalElectionListUnion>
            {
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(ListUnionIdStGallenProportionalElectionInContestBund),
                        Position = 1,
                        Description = LanguageUtil.MockAllLanguages("Listenverbindung 1"),
                    },
            },
        };

    public static ProportionalElection BundProportionalElectionInContestStGallen
        => new ProportionalElection
        {
            Id = Guid.Parse(IdBundProportionalElectionInContestStGallen),
            PoliticalBusinessNumber = "100",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl Bund"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl Bund"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidBund,
            ContestId = ContestMockedData.StGallenEvotingContest.Id,
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 0,
            AutomaticBallotBundleNumberGeneration = false,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = true,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.HagenbachBischoff,
            NumberOfMandates = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
            ProportionalElectionLists = new List<ProportionalElectionList>
            {
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(ListIdBundProportionalElectionInContestStGallen),
                        Position = 1,
                        BlankRowCount = 2,
                        OrderNumber = "1a",
                        Description = LanguageUtil.MockAllLanguages("Liste 1"),
                        ShortDescription = LanguageUtil.MockAllLanguages("Liste 1"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateId1BundProportionalElectionInContestStGallen),
                                FirstName = "firstName",
                                LastName = "lastName",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                Occupation = LanguageUtil.MockAllLanguages("occupation"),
                                OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Accumulated = true,
                                AccumulatedPosition = 2,
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                PartyId = DomainOfInfluenceMockedData.GuidPartyBundAndere,
                                Origin = "origin",
                                CheckDigit = 6,
                            },
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateId2BundProportionalElectionInContestStGallen),
                                FirstName = "firstName 2",
                                LastName = "lastName 2",
                                PoliticalFirstName = "pol first name 2",
                                PoliticalLastName = "pol last name 2",
                                Occupation = LanguageUtil.MockAllLanguages("occupation 2"),
                                OccupationTitle = LanguageUtil.MockAllLanguages("occupation title 2"),
                                DateOfBirth = new DateTime(1980, 3, 27, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = false,
                                Position = 3,
                                Locality = "locality 2",
                                Number = "number2",
                                Sex = SexType.Undefined,
                                Title = "title2",
                                ZipCode = "zip code2",
                                PartyId = DomainOfInfluenceMockedData.GuidPartyBundAndere,
                                Origin = "origin 2",
                                CheckDigit = 4,
                            },
                        },
                    },
            },
            ProportionalElectionListUnions = new List<ProportionalElectionListUnion>
            {
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(ListUnionIdBundProportionalElectionInContestStGallen),
                        Position = 1,
                        Description = LanguageUtil.MockAllLanguages("Listenverbindung 1"),
                        ProportionalElectionListUnionEntries = new List<ProportionalElectionListUnionEntry>
                        {
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId = Guid.Parse(ListIdBundProportionalElectionInContestStGallen),
                            },
                        },
                    },
            },
        };

    public static ProportionalElection UzwilProportionalElectionInContestStGallen
        => new ProportionalElection
        {
            Id = Guid.Parse(IdUzwilProportionalElectionInContestStGallen),
            PoliticalBusinessNumber = "166",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl Uzwil"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl Uzwil"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidUzwil,
            ContestId = ContestMockedData.StGallenEvotingContest.Id,
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = false,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 10,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.ContinuousForAllBundles,
            CandidateCheckDigit = true,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.HagenbachBischoff,
            NumberOfMandates = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
            ProportionalElectionLists = new List<ProportionalElectionList>
            {
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(ListIdUzwilProportionalElectionInContestStGallen),
                        Position = 1,
                        BlankRowCount = 0,
                        OrderNumber = "1a",
                        Description = LanguageUtil.MockAllLanguages("Liste 1"),
                        ShortDescription = LanguageUtil.MockAllLanguages("L1"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateIdUzwilProportionalElectionInContestStGallen),
                                FirstName = "firstName",
                                LastName = "lastName",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                Occupation = LanguageUtil.MockAllLanguages("occupation"),
                                OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Accumulated = true,
                                AccumulatedPosition = 2,
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                PartyId = DomainOfInfluenceMockedData.GuidPartyStGallenSVP,
                                Origin = "origin",
                                CheckDigit = 6,
                            },
                        },
                    },
            },
            ProportionalElectionListUnions = new List<ProportionalElectionListUnion>
            {
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(ListUnionIdUzwilProportionalElectionInContestStGallen),
                        Position = 1,
                        Description = LanguageUtil.MockAllLanguages("Listenverbindung 1"),
                    },
            },
        };

    public static ProportionalElection StGallenProportionalElectionInContestStGallen
        => new ProportionalElection
        {
            Id = Guid.Parse(IdStGallenProportionalElectionInContestStGallen),
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
            CandidateCheckDigit = false,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.HagenbachBischoff,
            NumberOfMandates = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
            ProportionalElectionLists = new List<ProportionalElectionList>
            {
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(ListIdStGallenProportionalElectionInContestStGallen),
                        Position = 1,
                        BlankRowCount = 1,
                        OrderNumber = "1a",
                        Description = LanguageUtil.MockAllLanguages("Liste 1"),
                        ShortDescription = LanguageUtil.MockAllLanguages("Liste 1"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateIdStGallenProportionalElectionInContestStGallen),
                                FirstName = "firstName",
                                LastName = "lastName",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                Occupation = LanguageUtil.MockAllLanguages("occupation"),
                                OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Accumulated = true,
                                AccumulatedPosition = 2,
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                PartyId = DomainOfInfluenceMockedData.GuidPartyBundAndere,
                                Origin = "origin",
                                CheckDigit = 6,
                            },
                        },
                    },
            },
        };

    public static ProportionalElection GossauProportionalElectionInContestStGallen
        => new ProportionalElection
        {
            Id = Guid.Parse(IdGossauProportionalElectionInContestStGallen),
            PoliticalBusinessNumber = "321",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl Gossau"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl Gossau"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGossau,
            ContestId = ContestMockedData.StGallenEvotingContest.Id,
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 10,
            BallotBundleSampleSize = 1,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = true,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.HagenbachBischoff,
            NumberOfMandates = 3,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = false,
            ProportionalElectionLists = new List<ProportionalElectionList>
            {
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(ListId1GossauProportionalElectionInContestStGallen),
                        Position = 1,
                        BlankRowCount = 0,
                        OrderNumber = "1a",
                        Description = LanguageUtil.MockAllLanguages("Liste 1"),
                        ShortDescription = LanguageUtil.MockAllLanguages("Liste 1"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateId1GossauProportionalElectionInContestStGallen),
                                FirstName = "firstName",
                                LastName = "lastName",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                Occupation = LanguageUtil.MockAllLanguages("occupation"),
                                OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Accumulated = true,
                                AccumulatedPosition = 2,
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                PartyId = DomainOfInfluenceMockedData.GuidPartyGossauFLiG,
                                Origin = "origin",
                                CheckDigit = 6,
                            },
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateId2GossauDeletedPartyProportionalElectionInContestStGallen),
                                FirstName = "candidate",
                                LastName = "number 2",
                                PoliticalFirstName = "pol first name 2",
                                PoliticalLastName = "pol last name 2",
                                Occupation = LanguageUtil.MockAllLanguages("occupation 2"),
                                OccupationTitle = LanguageUtil.MockAllLanguages("occupation title 2"),
                                DateOfBirth = new DateTime(1940, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = false,
                                Position = 3,
                                Accumulated = false,
                                Locality = "locality 2",
                                Number = "number2",
                                Sex = SexType.Undefined,
                                Title = "title 2",
                                PartyId = DomainOfInfluenceMockedData.GuidPartyGossauDeleted,
                                Origin = "origin 2",
                                CheckDigit = 4,
                            },
                        },
                    },
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(ListId2GossauProportionalElectionInContestStGallen),
                        Position = 2,
                        BlankRowCount = 0,
                        OrderNumber = "2",
                        Description = LanguageUtil.MockAllLanguages("Liste 2"),
                        ShortDescription = LanguageUtil.MockAllLanguages("Liste 2"),
                    },
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(ListId3GossauProportionalElectionInContestStGallen),
                        Position = 3,
                        BlankRowCount = 3,
                        OrderNumber = "3a",
                        Description = LanguageUtil.MockAllLanguages("Liste 3"),
                        ShortDescription = LanguageUtil.MockAllLanguages("Liste 3"),
                    },
            },
            ProportionalElectionListUnions = new List<ProportionalElectionListUnion>
            {
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(ListUnion1IdGossauProportionalElectionInContestStGallen),
                        Position = 1,
                        Description = LanguageUtil.MockAllLanguages("Listenverbindung 1"),
                        ProportionalElectionListUnionEntries = new List<ProportionalElectionListUnionEntry>
                        {
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId = Guid.Parse(ListId1GossauProportionalElectionInContestStGallen),
                            },
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId = Guid.Parse(ListId2GossauProportionalElectionInContestStGallen),
                            },
                        },
                    },
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(ListUnion2IdGossauProportionalElectionInContestStGallen),
                        Position = 2,
                        Description = LanguageUtil.MockAllLanguages("Listenverbindung 2"),
                        ProportionalElectionListUnionEntries = new List<ProportionalElectionListUnionEntry>
                        {
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId = Guid.Parse(ListId1GossauProportionalElectionInContestStGallen),
                            },
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId = Guid.Parse(ListId2GossauProportionalElectionInContestStGallen),
                            },
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId = Guid.Parse(ListId3GossauProportionalElectionInContestStGallen),
                            },
                        },
                    },
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(ListUnion3IdGossauProportionalElectionInContestStGallen),
                        Position = 3,
                        Description = LanguageUtil.MockAllLanguages("Listenverbindung 3"),
                    },
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(SubListUnion11IdGossauProportionalElectionInContestStGallen),
                        Position = 1,
                        Description = LanguageUtil.MockAllLanguages("Unterlistenverbindung 1.1"),
                        ProportionalElectionRootListUnionId = Guid.Parse(ListUnion1IdGossauProportionalElectionInContestStGallen),
                        ProportionalElectionListUnionEntries = new List<ProportionalElectionListUnionEntry>
                        {
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId = Guid.Parse(ListId1GossauProportionalElectionInContestStGallen),
                            },
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId = Guid.Parse(ListId2GossauProportionalElectionInContestStGallen),
                            },
                        },
                        ProportionalElectionMainListId = Guid.Parse(ListId1GossauProportionalElectionInContestStGallen),
                    },
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(SubListUnion12IdGossauProportionalElectionInContestStGallen),
                        Position = 2,
                        Description = LanguageUtil.MockAllLanguages("Unterlistenverbindung 1.2"),
                        ProportionalElectionRootListUnionId = Guid.Parse(ListUnion1IdGossauProportionalElectionInContestStGallen),
                        ProportionalElectionListUnionEntries = new List<ProportionalElectionListUnionEntry>
                        {
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId = Guid.Parse(ListId2GossauProportionalElectionInContestStGallen),
                            },
                        },
                        ProportionalElectionMainListId = Guid.Parse(ListId2GossauProportionalElectionInContestStGallen),
                    },
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(SubListUnion21IdGossauProportionalElectionInContestStGallen),
                        Position = 1,
                        Description = LanguageUtil.MockAllLanguages("Unterlistenverbindung 2.1"),
                        ProportionalElectionRootListUnionId = Guid.Parse(ListUnion2IdGossauProportionalElectionInContestStGallen),
                        ProportionalElectionListUnionEntries = new List<ProportionalElectionListUnionEntry>
                        {
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId = Guid.Parse(ListId1GossauProportionalElectionInContestStGallen),
                            },
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId = Guid.Parse(ListId2GossauProportionalElectionInContestStGallen),
                            },
                        },
                        ProportionalElectionMainListId = Guid.Parse(ListId1GossauProportionalElectionInContestStGallen),
                    },
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(SubListUnion22IdGossauProportionalElectionInContestStGallen),
                        Position = 2,
                        Description = LanguageUtil.MockAllLanguages("Unterlistenverbindung 2.2"),
                        ProportionalElectionRootListUnionId = Guid.Parse(ListUnion2IdGossauProportionalElectionInContestStGallen),
                        ProportionalElectionListUnionEntries = new List<ProportionalElectionListUnionEntry>
                        {
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId = Guid.Parse(ListId2GossauProportionalElectionInContestStGallen),
                            },
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId = Guid.Parse(ListId3GossauProportionalElectionInContestStGallen),
                            },
                        },
                        ProportionalElectionMainListId = Guid.Parse(ListId3GossauProportionalElectionInContestStGallen),
                    },
            },
        };

    public static ProportionalElection GossauProportionalElectionInContestBund
        => new ProportionalElection
        {
            Id = Guid.Parse(IdGossauProportionalElectionInContestBund),
            PoliticalBusinessNumber = "645",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl Gossau"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl Gossau"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGossau,
            ContestId = ContestMockedData.BundContest.Id,
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 10,
            BallotBundleSampleSize = 1,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = true,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.HagenbachBischoff,
            NumberOfMandates = 3,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = false,
            ProportionalElectionLists = new List<ProportionalElectionList>
            {
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(ListId1GossauProportionalElectionInContestBund),
                        Position = 1,
                        BlankRowCount = 0,
                        OrderNumber = "1a",
                        Description = LanguageUtil.MockAllLanguages("Liste 1"),
                        ShortDescription = LanguageUtil.MockAllLanguages("Liste 1"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateId1GossauProportionalElectionInContestBund),
                                FirstName = "firstName",
                                LastName = "lastName",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                Occupation = LanguageUtil.MockAllLanguages("occupation"),
                                OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Accumulated = true,
                                AccumulatedPosition = 2,
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                PartyId = DomainOfInfluenceMockedData.GuidPartyBundAndere,
                                Origin = "origin",
                                CheckDigit = 6,
                            },
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateId2GossauProportionalElectionInContestBund),
                                FirstName = "candidate",
                                LastName = "number 2",
                                PoliticalFirstName = "pol first name 2",
                                PoliticalLastName = "pol last name 2",
                                Occupation = LanguageUtil.MockAllLanguages("occupation 2"),
                                OccupationTitle = LanguageUtil.MockAllLanguages("occupation title 2"),
                                DateOfBirth = new DateTime(1940, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = false,
                                Position = 3,
                                Accumulated = false,
                                Locality = "locality 2",
                                Number = "number2",
                                Sex = SexType.Undefined,
                                Title = "title 2",
                                PartyId = DomainOfInfluenceMockedData.GuidPartyStGallenSVP,
                                Origin = "origin 2",
                                CheckDigit = 4,
                            },
                        },
                    },
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(ListId2GossauProportionalElectionInContestBund),
                        Position = 2,
                        BlankRowCount = 0,
                        OrderNumber = "2",
                        Description = LanguageUtil.MockAllLanguages("Liste 2"),
                        ShortDescription = LanguageUtil.MockAllLanguages("Liste 2"),
                    },
            },
            ProportionalElectionListUnions = new List<ProportionalElectionListUnion>
            {
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(ListUnionIdGossauProportionalElectionInContestBund),
                        Position = 1,
                        Description = LanguageUtil.MockAllLanguages("Listenverbindung 1"),
                        ProportionalElectionListUnionEntries = new List<ProportionalElectionListUnionEntry>
                        {
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId = Guid.Parse(ListId1GossauProportionalElectionInContestBund),
                            },
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId = Guid.Parse(ListId2GossauProportionalElectionInContestBund),
                            },
                        },
                    },
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(SubListUnionIdGossauProportionalElectionInContestBund),
                        Position = 1,
                        Description = LanguageUtil.MockAllLanguages("Unterlistenverbindung 1.1"),
                        ProportionalElectionRootListUnionId = Guid.Parse(ListUnionIdGossauProportionalElectionInContestBund),
                        ProportionalElectionListUnionEntries = new List<ProportionalElectionListUnionEntry>
                        {
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId = Guid.Parse(ListId1GossauProportionalElectionInContestBund),
                            },
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId = Guid.Parse(ListId2GossauProportionalElectionInContestBund),
                            },
                        },
                        ProportionalElectionMainListId = Guid.Parse(ListId1GossauProportionalElectionInContestBund),
                    },
            },
        };

    public static ProportionalElection StGallenProportionalElectionInContestStGallenWithoutChilds
        => new ProportionalElection
        {
            Id = Guid.Parse(IdStGallenProportionalElectionInContestStGallenWithoutChilds),
            PoliticalBusinessNumber = "500",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl St.Gallen 2"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl St.Gallen 2"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidStGallen,
            ContestId = ContestMockedData.StGallenEvotingContest.Id,
            Active = false,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 3,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = true,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.HagenbachBischoff,
            NumberOfMandates = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
        };

    public static ProportionalElection GossauProportionalElectionInContestGossau
        => new ProportionalElection
        {
            Id = Guid.Parse(IdGossauProportionalElectionInContestGossau),
            PoliticalBusinessNumber = "324",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl Gossau"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl Gossau"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGossau,
            ContestId = ContestMockedData.GossauContest.Id,
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 5,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = true,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.HagenbachBischoff,
            NumberOfMandates = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
            ProportionalElectionLists = new List<ProportionalElectionList>
            {
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(ListIdGossauProportionalElectionInContestGossau),
                        Position = 1,
                        BlankRowCount = 0,
                        OrderNumber = "1a",
                        Description = LanguageUtil.MockAllLanguages("Liste 1"),
                        ShortDescription = LanguageUtil.MockAllLanguages("Liste 1"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateIdGossauProportionalElectionInContestGossau),
                                FirstName = "firstName",
                                LastName = "lastName",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                Occupation = LanguageUtil.MockAllLanguages("occupation"),
                                OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Accumulated = true,
                                AccumulatedPosition = 2,
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                PartyId = DomainOfInfluenceMockedData.GuidPartyBundAndere,
                                Origin = "origin",
                                CheckDigit = 6,
                            },
                        },
                    },
            },
            ProportionalElectionListUnions = new List<ProportionalElectionListUnion>
            {
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(ListUnionIdGossauProportionalElectionInContestGossau),
                        Position = 1,
                        Description = LanguageUtil.MockAllLanguages("Listenverbindung 1"),
                    },
            },
        };

    public static ProportionalElection UzwilProportionalElectionInContestUzwil
        => new ProportionalElection
        {
            Id = Guid.Parse(IdUzwilProportionalElectionInContestUzwilWithoutChilds),
            PoliticalBusinessNumber = "412",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl Uzwil"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl Uzwil"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidUzwil,
            ContestId = ContestMockedData.UzwilEvotingContest.Id,
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 3,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = true,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            NumberOfMandates = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
            ProportionalElectionLists = new List<ProportionalElectionList>
            {
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(ListIdUzwilProportionalElectionInContestUzwil),
                        Position = 1,
                        BlankRowCount = 0,
                        OrderNumber = "1a",
                        Description = LanguageUtil.MockAllLanguages("Liste 1"),
                        ShortDescription = LanguageUtil.MockAllLanguages("Liste 1"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateIdUzwilProportionalElectionInContestUzwil),
                                FirstName = "firstName",
                                LastName = "lastName",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                Occupation = LanguageUtil.MockAllLanguages("occupation"),
                                OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Accumulated = true,
                                AccumulatedPosition = 2,
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                PartyId = DomainOfInfluenceMockedData.GuidPartyBundAndere,
                                Origin = "origin",
                                CheckDigit = 6,
                            },
                        },
                    },
            },
        };

    public static ProportionalElection UzwilProportionalElectionInContestBundWithoutChilds
        => new ProportionalElection
        {
            Id = Guid.Parse(IdUzwilProportionalElectionInContestBund),
            PoliticalBusinessNumber = "714",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl Uzwil"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl Uzwil"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidUzwil,
            Active = false,
            ContestId = ContestMockedData.BundContest.Id,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = true,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            NumberOfMandates = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
        };

    public static ProportionalElection GenfProportionalElectionInContestBundWithoutChilds
        => new ProportionalElection
        {
            Id = Guid.Parse(IdGenfProportionalElectionInContestBundWithoutChilds),
            PoliticalBusinessNumber = "714a",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl Genf"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl Genf"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGenf,
            ContestId = ContestMockedData.BundContest.Id,
            Active = false,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = true,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            NumberOfMandates = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
        };

    public static ProportionalElection KircheProportionalElectionInContestKirche
        => new ProportionalElection
        {
            Id = Guid.Parse(IdKircheProportionalElectionInContestKirche),
            PoliticalBusinessNumber = "aaa",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl Kirche"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl Kirche"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidKirchgemeinde,
            ContestId = ContestMockedData.KirchenContest.Id,
            Active = false,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 4,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = true,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.HagenbachBischoff,
            NumberOfMandates = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
            ProportionalElectionLists = new List<ProportionalElectionList>
            {
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(ListIdKircheProportionalElectionInContestKirche),
                        Position = 1,
                        BlankRowCount = 0,
                        OrderNumber = "1a",
                        Description = LanguageUtil.MockAllLanguages("Liste 1"),
                        ShortDescription = LanguageUtil.MockAllLanguages("Liste 1"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateIdKircheProportionalElectionInContestKirche),
                                FirstName = "firstName",
                                LastName = "lastName",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                Occupation = LanguageUtil.MockAllLanguages("occupation"),
                                OccupationTitle = LanguageUtil.MockAllLanguages("occupation title"),
                                DateOfBirth = new DateTime(1970, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = false,
                                Position = 1,
                                Accumulated = true,
                                AccumulatedPosition = 2,
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Male,
                                Title = "title",
                                PartyId = DomainOfInfluenceMockedData.GuidPartyKirchgemeindeEVP,
                                Origin = "origin",
                                CheckDigit = 6,
                            },
                        },
                    },
            },
            ProportionalElectionListUnions = new List<ProportionalElectionListUnion>
            {
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(ListUnionIdKircheProportionalElectionInContestKirche),
                        Position = 1,
                        Description = LanguageUtil.MockAllLanguages("Listenverbindung 1 Kirche"),
                    },
            },
        };

    public static ProportionalElection KircheProportionalElectionInContestKircheWithoutChilds
        => new ProportionalElection
        {
            Id = Guid.Parse(IdKircheProportionalElectionInContestKircheWithoutChilds),
            PoliticalBusinessNumber = "aaa",
            OfficialDescription = LanguageUtil.MockAllLanguages("Proporzwahl Kirche ohne Listen"),
            ShortDescription = LanguageUtil.MockAllLanguages("Proporzwahl Kirche ohne Listen"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidKirchgemeinde,
            ContestId = ContestMockedData.KirchenContest.Id,
            Active = false,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 2,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = true,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            NumberOfMandates = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Physically,
            EnforceReviewProcedureForCountingCircles = false,
            EnforceCandidateCheckDigitForCountingCircles = false,
        };

    public static IEnumerable<ProportionalElection> All
    {
        get
        {
            yield return BundProportionalElectionInContestBund;
            yield return BundProportionalElectionInContestStGallen;
            yield return UzwilProportionalElectionInContestStGallen;
            yield return StGallenProportionalElectionInContestBund;
            yield return StGallenProportionalElectionInContestStGallen;
            yield return GossauProportionalElectionInContestStGallen;
            yield return GossauProportionalElectionInContestBund;
            yield return StGallenProportionalElectionInContestStGallenWithoutChilds;
            yield return GossauProportionalElectionInContestGossau;
            yield return UzwilProportionalElectionInContestUzwil;
            yield return UzwilProportionalElectionInContestBundWithoutChilds;
            yield return GenfProportionalElectionInContestBundWithoutChilds;
            yield return KircheProportionalElectionInContestKirche;
            yield return KircheProportionalElectionInContestKircheWithoutChilds;
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
            var simplePbBuilder = sp.GetRequiredService<SimplePoliticalBusinessBuilder<ProportionalElection>>();
            var db = sp.GetRequiredService<DataContext>();
            db.ProportionalElections.AddRange(All);
            await db.SaveChangesAsync();

            foreach (var election in All)
            {
                await simplePbBuilder.Create(election);
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
            await ProportionalElectionUnionMockedData.Seed(runScoped);
        }
    }

    private static ProportionalElectionAggregate ToAggregate(
        ProportionalElection proportionalElection,
        IAggregateFactory aggregateFactory,
        TestMapper mapper)
    {
        var aggregate = aggregateFactory.New<ProportionalElectionAggregate>();
        var election = mapper.Map<Core.Domain.ProportionalElection>(proportionalElection);
        var doi = DomainOfInfluenceMockedData.All.First(x => x.Id == proportionalElection.DomainOfInfluenceId);

        aggregate.CreateFrom(election);

        foreach (var list in proportionalElection.ProportionalElectionLists)
        {
            list.ProportionalElectionId = aggregate.Id;
            var protoList = mapper.Map<Core.Domain.ProportionalElectionList>(list);
            aggregate.CreateListFrom(protoList);

            foreach (var candidate in list.ProportionalElectionCandidates)
            {
                var protoCandidate = mapper.Map<Core.Domain.ProportionalElectionCandidate>(candidate);
                protoCandidate.ProportionalElectionListId = protoList.Id;
                aggregate.CreateCandidateFrom(protoCandidate, doi.Type);
            }
        }

        foreach (var listUnion in proportionalElection.ProportionalElectionListUnions)
        {
            listUnion.ProportionalElectionId = aggregate.Id;
            var domainListUnion = mapper.Map<Core.Domain.ProportionalElectionListUnion>(listUnion);
            aggregate.CreateListUnionFrom(domainListUnion);

            var protoListUnionEntries = new ProportionalElectionListUnionEntries { ProportionalElectionListUnionId = listUnion.Id };
            protoListUnionEntries.ProportionalElectionListIds.AddRange(
                listUnion.ProportionalElectionListUnionEntries.Select(e => e.ProportionalElectionListId));
            aggregate.UpdateListUnionEntriesFrom(protoListUnionEntries);

            if (listUnion.ProportionalElectionMainListId.HasValue)
            {
                aggregate.UpdateListUnionMainList(listUnion.Id, listUnion.ProportionalElectionMainListId);
            }
        }

        return aggregate;
    }
}
