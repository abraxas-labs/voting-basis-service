// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Voting.Basis.Core.Extensions;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Lib.Common;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Models;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.MockedData;

public static class EventLogMockedData
{
    private const string CcId = "eae2cfaf-c787-48b9-a108-c975b0a580da";
    private const string CcForeignTenantId = "8bc1e177-d2ca-4f39-aad3-d6cb98e031a6";
    private const string DoiId = "3c3f3ae2-0439-4998-85ff-ae1f7eac94a3";
    private const string ContestId = "239702ef-3064-498c-beea-ebf57a55ff05";
    private const string VoteId = "5483076b-e596-44d3-b34e-6e9220eed84c";

    public static ProtoModels.CountingCircle Cc()
    {
        return new ProtoModels.CountingCircle
        {
            Name = "Uzwil",
            Bfs = "1234",
            Id = CcId,
            ResponsibleAuthority = new ProtoModels.Authority
            {
                Name = "Uzwil",
                Email = "uzwil-test@abraxas.ch",
                Phone = "071 123 12 20",
                Street = "WerkstrasseX",
                City = "MyCityX",
                Zip = "9200",
                SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
            },
            ContactPersonSameDuringEventAsAfter = false,
            ContactPersonDuringEvent = new ProtoModels.ContactPerson
            {
                Email = "uzwil-test@abraxas.ch",
                Phone = "071 123 12 21",
                MobilePhone = "071 123 12 31",
                FamilyName = "Muster",
                FirstName = "Hans",
            },
            ContactPersonAfterEvent = new ProtoModels.ContactPerson
            {
                Email = "uzwil-test2@abraxas.ch",
                Phone = "071 123 12 22",
                MobilePhone = "071 123 12 33",
                FamilyName = "Wichtig",
                FirstName = "Rudolph",
            },
            State = SharedProto.CountingCircleState.Active,
        };
    }

    public static ProtoModels.DomainOfInfluence Doi()
    {
        return new ProtoModels.DomainOfInfluence
        {
            Id = DoiId,
            Canton = SharedProto.DomainOfInfluenceCanton.Sg,
            Name = "Bezirk Uzwil",
            ShortName = "BZ Uz",
            SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
            Type = SharedProto.DomainOfInfluenceType.Bz,
            ContactPerson = new ProtoModels.ContactPerson
            {
                Email = "uzwil-test@abraxas.ch",
                Phone = "071 123 12 21",
                MobilePhone = "071 123 12 31",
                FamilyName = "Muster",
                FirstName = "Hans",
            },
        };
    }

    public static ProtoModels.Contest Contest()
    {
        return new ProtoModels.Contest
        {
            Id = ContestId,
            Date = new DateTime(2020, 8, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            Description = { { Languages.German, "Urnengang vom 08.02.2021" } },
            DomainOfInfluenceId = DoiId,
            EndOfTestingPhase = new DateTime(2019, 1, 1, 12, 45, 0, DateTimeKind.Utc).ToTimestamp(),
            State = SharedProto.ContestState.TestingPhase,
        };
    }

    public static ProtoModels.Vote Vote()
    {
        return new ProtoModels.Vote
        {
            Id = VoteId,
            PoliticalBusinessNumber = "2000",
            OfficialDescription = { { Languages.German, "Neue Abstimmung 1" } },
            ShortDescription = { { Languages.German, "Neue Abstimmung 1" } },
            DomainOfInfluenceId = DoiId,
            ContestId = ContestId,
            ResultEntry = SharedProto.VoteResultEntry.FinalResults,
            ResultAlgorithm = SharedProto.VoteResultAlgorithm.PopularMajority,
            AutomaticBallotBundleNumberGeneration = false,
            BallotBundleSampleSizePercent = 0,
            EnforceResultEntryForCountingCircles = true,
            ReviewProcedure = SharedProto.VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
        };
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
        {
            var eventPublisher = sp.GetRequiredService<TestEventPublisher>();
            var mapper = sp.GetRequiredService<TestMapper>();

            var cc = Cc();
            var ev0 = new CountingCircleCreated
            {
                CountingCircle = mapper.Map<CountingCircleEventData>(cc),
                EventInfo = GetMockedEventInfo(0),
            };

            cc.Name = "Uzwil Updated";
            var ev1 = new CountingCircleUpdated
            {
                CountingCircle = mapper.Map<CountingCircleEventData>(cc),
                EventInfo = GetMockedEventInfo(10),
            };

            var doi = Doi();
            var ev2 = new DomainOfInfluenceCreated
            {
                DomainOfInfluence = mapper.Map<DomainOfInfluenceEventData>(doi),
                EventInfo = GetMockedEventInfo(30),
            };

            doi.Name = "Bezirk Uzwil Updated";
            var ev3 = new DomainOfInfluenceUpdated
            {
                DomainOfInfluence = mapper.Map<DomainOfInfluenceEventData>(doi),
                EventInfo = GetMockedEventInfo(60),
            };

            var contest = Contest();
            var ev4 = new ContestCreated
            {
                Contest = mapper.Map<ContestEventData>(contest),
                EventInfo = GetMockedEventInfo(90),
            };

            var ev5 = new DomainOfInfluenceCountingCircleEntriesUpdated
            {
                DomainOfInfluenceCountingCircleEntries = new DomainOfInfluenceCountingCircleEntriesEventData
                {
                    Id = DoiId,
                    CountingCircleIds =
                    {
                            CcId,
                    },
                },
                EventInfo = GetMockedEventInfo(120),
            };

            var vote = Vote();
            var ev6 = new VoteCreated
            {
                Vote = mapper.Map<VoteEventData>(vote),
                EventInfo = GetMockedEventInfo(150),
            };

            vote.ShortDescription[Languages.German] = "Neue Abstimmung 1 Updated";
            var ev7 = new VoteUpdated
            {
                Vote = mapper.Map<VoteEventData>(vote),
                EventInfo = GetMockedEventInfo(180),
            };

            var ccForeignTenant = Cc();
            ccForeignTenant.Name = "St. Gallen";
            ccForeignTenant.Id = CcForeignTenantId;
            var ev8 = new CountingCircleCreated
            {
                CountingCircle = mapper.Map<CountingCircleEventData>(ccForeignTenant),
                EventInfo = GetMockedEventInfo(210, SecureConnectTestDefaults.MockedTenantStGallen),
            };

            await eventPublisher.Publish(0, ev0);
            await eventPublisher.Publish(1, ev1);
            await eventPublisher.Publish(2, ev2);
            await eventPublisher.Publish(3, ev3);
            await eventPublisher.Publish(4, ev4);
            await eventPublisher.Publish(5, ev5);
            await eventPublisher.Publish(6, ev6);
            await eventPublisher.Publish(7, ev7);
            await eventPublisher.Publish(8, ev8);
        });
    }

    private static EventInfo GetMockedEventInfo(long additionalDays, Tenant? tenant = null)
    {
        return new EventInfo
        {
            Timestamp = new Timestamp
            {
                Seconds = 1594980476 + (additionalDays * 24 * 60 * 60),
            },
            Tenant = (tenant ?? SecureConnectTestDefaults.MockedTenantDefault).ToEventInfoTenant(),
            User = SecureConnectTestDefaults.MockedUserDefault.ToEventInfoUser(),
        };
    }
}
