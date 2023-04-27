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
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.MockedData;

public static class HistoricalMockedData
{
    public const string CcStGallenId = "736ec87f-9953-4946-8459-1d03bc237aa1";
    public const string CcGossauId = "14451d15-cd48-46d1-bfed-6d069d160886";
    public const string CcFrauenfeldId = "43960c28-476a-484f-9e63-8743403b772d";

    public const string CcIdJona = "3ec3d50c-6cb7-4ba1-bd1a-2882153834b0";
    public const string CcIdRapperswil = "2a3a799a-2437-47da-86fe-06decf70a267";
    public const string CcIdRapperswilJona = "d6e7bc38-c60f-42eb-8613-3c0267f651a8";
    public const string CcMergeIdRapperswilJona = "81d72669-d1e9-45c6-bdd0-08c25b1e9811";

    public const string DoiBundId = "8e54ec7b-7e0d-43cb-8cbf-0531efae804a";
    public const string DoiStGallenId = "c34f2f41-fca6-4248-86d7-091379cfd991";
    public const string DoiThurgauId = "ec1e23a1-bfba-4f1b-b75f-ca31ac53a80f";
    public const string DoiRapperswilJonaId = "63c37d33-e6c9-4ecb-84cc-34dbe083cfb3";

    public static readonly DateTime DateTimeBeforeEvents = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DateTimeAfterEvent0 = new DateTime(2020, 7, 18, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DateTimeAfterEvent1 = new DateTime(2020, 7, 18, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DateTimeAfterEvent2 = new DateTime(2020, 8, 17, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DateTimeAfterEvent3 = new DateTime(2020, 9, 16, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DateTimeAfterEvent4 = new DateTime(2020, 10, 16, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DateTimeAfterEvent5 = new DateTime(2020, 11, 15, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DateTimeAfterEvent6 = new DateTime(2020, 11, 15, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DateTimeAfterEvent7 = new DateTime(2020, 12, 15, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DateTimeAfterEvent8 = new DateTime(2021, 1, 14, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DateTimeAfterEvent9 = new DateTime(2021, 1, 14, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DateTimeAfterEvent10 = new DateTime(2021, 2, 13, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DateTimeAfterEvent11 = new DateTime(2021, 3, 15, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DateTimeAfterEvent12 = new DateTime(2021, 4, 14, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DateTimeAfterEvent13 = new DateTime(2021, 5, 14, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DateTimeAfterEvent14 = new DateTime(2021, 6, 14, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DateTimeAfterEvent15 = new DateTime(2021, 7, 14, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DateTimeAfterEvent16 = new DateTime(2021, 7, 14, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DateTimeAfterEvent17 = new DateTime(2021, 8, 12, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DateTimeAfterEvent18 = new DateTime(2021, 9, 11, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DateTimeAfterEvent19 = new DateTime(2021, 9, 11, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DateTimeAfterEvent20 = new DateTime(2021, 9, 11, 0, 0, 0, DateTimeKind.Utc);

    public static ProtoModels.CountingCircle CcGossau()
    {
        return new ProtoModels.CountingCircle
        {
            Name = "Gossau",
            Id = CcGossauId,
            Bfs = "CC101",
            Code = "CC101c",
            ResponsibleAuthority = new ProtoModels.Authority
            {
                SecureConnectId = "rnd",
            },
            ContactPersonDuringEvent = new ProtoModels.ContactPerson(),
            ContactPersonAfterEvent = new ProtoModels.ContactPerson(),
            State = SharedProto.CountingCircleState.Active,
        };
    }

    public static ProtoModels.CountingCircle CcStGallen()
    {
        return new ProtoModels.CountingCircle
        {
            Name = "St. Gallen",
            Id = CcStGallenId,
            Bfs = "CC100",
            Code = "CC100c",
            ResponsibleAuthority = new ProtoModels.Authority
            {
                SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
            },
            ContactPersonDuringEvent = new ProtoModels.ContactPerson(),
            ContactPersonAfterEvent = new ProtoModels.ContactPerson(),
            State = SharedProto.CountingCircleState.Active,
        };
    }

    public static ProtoModels.CountingCircle CcFrauenfeld()
    {
        return new ProtoModels.CountingCircle
        {
            Name = "Frauenfeld",
            Id = CcFrauenfeldId,
            Bfs = "CC200",
            Code = "CC200c",
            ResponsibleAuthority = new ProtoModels.Authority
            {
                SecureConnectId = "rnd",
            },
            ContactPersonDuringEvent = new ProtoModels.ContactPerson(),
            ContactPersonAfterEvent = new ProtoModels.ContactPerson(),
            State = SharedProto.CountingCircleState.Active,
        };
    }

    public static ProtoModels.CountingCircle CcRapperswil()
    {
        return new ProtoModels.CountingCircle
        {
            Name = "Rapperswil",
            Id = CcIdRapperswil,
            Bfs = "none",
            Code = "nonec",
            ResponsibleAuthority = new ProtoModels.Authority
            {
                SecureConnectId = "random",
            },
            ContactPersonDuringEvent = new ProtoModels.ContactPerson(),
            ContactPersonAfterEvent = new ProtoModels.ContactPerson(),
            State = SharedProto.CountingCircleState.Active,
        };
    }

    public static ProtoModels.CountingCircle CcJona()
    {
        return new ProtoModels.CountingCircle
        {
            Name = "Jona",
            Id = CcIdJona,
            Bfs = "none",
            Code = "nonec",
            ResponsibleAuthority = new ProtoModels.Authority
            {
                SecureConnectId = "random",
            },
            ContactPersonDuringEvent = new ProtoModels.ContactPerson(),
            ContactPersonAfterEvent = new ProtoModels.ContactPerson(),
            State = SharedProto.CountingCircleState.Active,
        };
    }

    public static ProtoModels.CountingCirclesMerger CcMergeRapperswilJona()
    {
        return new ProtoModels.CountingCirclesMerger
        {
            Id = CcMergeIdRapperswilJona,
            ActiveFrom = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc).ToTimestamp(),
            CopyFromCountingCircleId = CcIdRapperswil,
            NewCountingCircle = new ProtoModels.CountingCircle
            {
                Name = "RapperswilJona",
                Id = CcIdRapperswilJona,
                Bfs = "none",
                Code = "nonec",
                ResponsibleAuthority = new ProtoModels.Authority
                {
                    SecureConnectId = "random",
                },
                State = SharedProto.CountingCircleState.Inactive,
                ContactPersonDuringEvent = new ProtoModels.ContactPerson(),
            },
            MergedCountingCircles =
                {
                    CcRapperswil(),
                    CcJona(),
                },
        };
    }

    public static DomainOfInfluenceEventData DoiBund()
    {
        return new DomainOfInfluenceEventData
        {
            Id = DoiBundId,
            Name = "Bund",
            ShortName = "Bund",
            SecureConnectId = "rnd",
            Type = SharedProto.DomainOfInfluenceType.Ch,
            Canton = SharedProto.DomainOfInfluenceCanton.Sg,
        };
    }

    public static DomainOfInfluenceEventData DoiStGallen()
    {
        return new DomainOfInfluenceEventData
        {
            Id = DoiStGallenId,
            Name = "St. Gallen",
            ShortName = "SG",
            SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
            Type = SharedProto.DomainOfInfluenceType.Ct,
            ParentId = DoiBundId,
        };
    }

    public static DomainOfInfluenceEventData DoiThurgau()
    {
        return new DomainOfInfluenceEventData
        {
            Id = DoiThurgauId,
            Name = "Thurgau",
            ShortName = "TG",
            SecureConnectId = "rnd",
            Type = SharedProto.DomainOfInfluenceType.Ct,
            ParentId = DoiBundId,
            ResponsibleForVotingCards = true,
        };
    }

    public static DomainOfInfluenceEventData DoiRapperswilJona()
    {
        return new DomainOfInfluenceEventData
        {
            Id = DoiRapperswilJonaId,
            Canton = SharedProto.DomainOfInfluenceCanton.Sg,
            Name = "Rapperswil",
            ShortName = "RW",
            SecureConnectId = "rnd",
            Type = SharedProto.DomainOfInfluenceType.Mu,
            ResponsibleForVotingCards = true,
        };
    }

    /*
    /// Dois Bund, Thugau and Ccs Frauenfeld and Gossau have a different tenant than the ElectionAdmin.
    /// Ev0 : 2020-07-17 Cc.Create St.Gallen.                             {Ccs: [Sg]}
    /// Ev1 : 2020-07-17 Cc.Create Frauenfeld (tenant="rnd").             {Ccs: [Sg, Ff]}
    /// Ev2 : 2020-08-16 Doi.Create Bund.                                 {Ccs: [Sg, Ff], Dois: [Bund]}
    /// Ev3 : 2020-09-15 Doi.Create St.Gallen (Child of Bund).            {Ccs: [Sg, Ff], Dois: [Bund, Sg]}
    /// Ev4 : 2020-10-15 Doi.Create Thurgau (Child of Bund).              {Ccs: [Sg, Ff], Dois: [Bund, Sg, Tg]}
    /// Ev5 : 2020-11-14 DoiCc.Assign to St.Gallen {St.Gallen}            {Ccs: [Sg, Ff], Dois: [Bund, Sg, Tg], DoiCcs: [{Bund, Sg}, {Sg, Sg}]}
    /// Ev6 : 2020-11-14 DoiCc.Assign to Thurgau {Frauenfeld}             {Ccs: [Sg, Ff], Dois: [Bund, Sg, Tg], DoiCcs: [{Bund, Sg}, {Sg, Sg}, {Bund, Ff}, {Tg, Ff}]}
    /// Ev7 : 2020-12-14 Cc.Create Gossau.                                {Ccs: [Sg, Ff, Gossau], Dois: [Bund, Sg, Tg], DoiCcs: [{Bund, Sg}, {Sg, Sg}, {Bund, Ff}, {Tg, Ff}]}
    /// Ev8 : 2021-01-13 Cc.Update Gossau EDITED.                         {Ccs: [Sg, Ff, Gossau EDITED], Dois: [Bund, Sg, Tg], DoiCcs: [{Bund, Sg}, {Sg, Sg}, {Bund, Ff}, {Tg, Ff}]}
    /// Ev9 : 2021-01-13 Doi.Update St.Gallen EDITED                      {Ccs: [Sg, Ff, Gossau EDITED], Dois: [Bund, Sg EDITED, Tg], DoiCcs: [{Bund, Sg}, {Sg EDITED, Sg}, {Bund, Ff}, {Tg, Ff}]}
    /// Ev10: 2021-02-12 DoiCc.Assign to St.Gallen EDITED {}.             {Ccs: [Sg, Ff, Gossau EDITED], Dois: [Bund, Sg EDITED, Tg], DoiCcs: [{Bund, Ff}, {Tg, Ff}]}
    /// Ev11: 2021-03-14 DoiCc.Assign to St.Gallen {St.Gallen, Gossau}.   {Ccs: [Sg, Ff, Gossau EDITED], Dois: [Bund, Sg EDITED, Tg], DoiCcs: [{Bund, Sg}, {Bund, Gossau EDITED}, {Sg EDITED, Sg}, {Sg EDITED, Gossau EDITED}, {Bund, Ff}, {Tg, Ff}]}
    /// Ev12: 2021-04-13 Cc.Delete Gossau.                                {Ccs: [Sg, Ff], Dois: [Bund, Sg EDITED, Tg], DoiCcs: [{Bund, Sg}, {Sg EDITED, Sg}, {Bund, Ff}, {Tg, Ff}]}
    /// Ev13: 2021-05-13 Doi.Delete St.Gallen.                            {Ccs: [Sg, Ff], Dois: [Bund, Tg], DoiCcs: [{Bund, Sg}, {Bund, Ff}, {Tg, Ff}]}
    /// Ev14: 2021-06-13 Doi.Delete Bund.                                 {Ccs: [Sg, Ff], Dois: [], DoiCcs: []}.
    /// Ev15: 2021-07-12 Cc.Create Jona                                   {Ccs: [Sg, Ff, J], Dois: [], DoiCcs: []}
    /// Ev16: 2021-07-12 Cc.Create Rapperswil                             {Ccs: [Sg, Ff, J, R], Dois: [], DoiCcs: []}
    /// Ev16: 2021-07-12 Doi.Create RapperswilJona                        {Ccs: [Sg, Ff, J, R], Dois: [Rj], DoiCcs: []}
    /// Ev10: 2021-02-12 DoiCc.Assign Rapperswil {Jona, Rapperswil}.      {Ccs: [Sg, Ff, J, R], Dois: [Rj], DoiCcs: [{J, R}]}
    /// Ev17: 2021-08-11 Cc.ScheduleMerge Rapperswil-Jona                 {Ccs: [Sg, Ff, J, R], Dois: [Rj], DoiCcs: [{J, R}]}
    /// Ev18: 2021-09-10 Cc.ActivateMerge Rapperswil-Jona                 {Ccs: [Sg, Ff, J, R, Rj], Dois: [Rj], DoiCcs: [{Rj}]}
    /// Ev19: 2021-09-10 Cc.SetMerged Rapperswil                          {Ccs: [Sg, Ff, J, Rj], Dois: [Rj], DoiCcs: [{Rj}]}
    /// Ev20: 2021-09-10 Cc.SetMerged Jona                                {Ccs: [Sg, Ff, Rj], Dois: [Rj], DoiCcs: [{Rj}]}
    */
    public static async Task SeedHistory(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
        {
            var mapper = sp.GetRequiredService<TestMapper>();
            var eventPublisher = sp.GetRequiredService<TestEventPublisher>();

            var ev0 = new CountingCircleCreated
            {
                CountingCircle = mapper.Map<CountingCircleEventData>(CcStGallen()),
                EventInfo = GetMockedEventInfo(0),
            };

            var ev1 = new CountingCircleCreated
            {
                CountingCircle = mapper.Map<CountingCircleEventData>(CcFrauenfeld()),
                EventInfo = GetMockedEventInfo(0),
            };

            var ev2 = new DomainOfInfluenceCreated
            {
                DomainOfInfluence = mapper.Map<DomainOfInfluenceEventData>(DoiBund()),
                EventInfo = GetMockedEventInfo(30),
            };

            var ev3 = new DomainOfInfluenceCreated
            {
                DomainOfInfluence = mapper.Map<DomainOfInfluenceEventData>(DoiStGallen()),
                EventInfo = GetMockedEventInfo(60),
            };

            var ev4 = new DomainOfInfluenceCreated
            {
                DomainOfInfluence = mapper.Map<DomainOfInfluenceEventData>(DoiThurgau()),
                EventInfo = GetMockedEventInfo(90),
            };

            var ev5 = new DomainOfInfluenceCountingCircleEntriesUpdated
            {
                DomainOfInfluenceCountingCircleEntries = new DomainOfInfluenceCountingCircleEntriesEventData
                {
                    Id = DoiStGallenId,
                    CountingCircleIds =
                    {
                            CcStGallenId,
                    },
                },
                EventInfo = GetMockedEventInfo(120),
            };

            var ev6 = new DomainOfInfluenceCountingCircleEntriesUpdated
            {
                DomainOfInfluenceCountingCircleEntries = new DomainOfInfluenceCountingCircleEntriesEventData
                {
                    Id = DoiThurgauId,
                    CountingCircleIds =
                    {
                            CcFrauenfeldId,
                    },
                },
                EventInfo = GetMockedEventInfo(120),
            };

            var ev7 = new CountingCircleCreated
            {
                CountingCircle = mapper.Map<CountingCircleEventData>(CcGossau()),
                EventInfo = GetMockedEventInfo(150),
            };

            var updateGossauCc = mapper.Map<CountingCircleEventData>(CcGossau());
            updateGossauCc.Name = "Gossau EDITED";
            var ev8 = new CountingCircleUpdated
            {
                CountingCircle = updateGossauCc,
                EventInfo = GetMockedEventInfo(180),
            };

            var updateStGallenDoi = mapper.Map<DomainOfInfluenceEventData>(DoiStGallen());
            updateStGallenDoi.Name = "StGallen EDITED";
            var ev9 = new DomainOfInfluenceUpdated
            {
                DomainOfInfluence = updateStGallenDoi,
                EventInfo = GetMockedEventInfo(180),
            };

            var ev10 = new DomainOfInfluenceCountingCircleEntriesUpdated
            {
                DomainOfInfluenceCountingCircleEntries = new DomainOfInfluenceCountingCircleEntriesEventData
                {
                    Id = DoiStGallenId,
                },
                EventInfo = GetMockedEventInfo(210),
            };

            var ev11 = new DomainOfInfluenceCountingCircleEntriesUpdated
            {
                DomainOfInfluenceCountingCircleEntries = new DomainOfInfluenceCountingCircleEntriesEventData
                {
                    Id = DoiStGallenId,
                    CountingCircleIds =
                    {
                            CcStGallenId,
                            CcGossauId,
                    },
                },
                EventInfo = GetMockedEventInfo(240),
            };

            var ev12 = new CountingCircleDeleted
            {
                CountingCircleId = CcGossauId,
                EventInfo = GetMockedEventInfo(270),
            };

            var ev13 = new DomainOfInfluenceDeleted
            {
                DomainOfInfluenceId = DoiStGallenId,
                EventInfo = GetMockedEventInfo(300),
            };

            var ev14 = new DomainOfInfluenceDeleted
            {
                DomainOfInfluenceId = DoiBundId,
                EventInfo = GetMockedEventInfo(330),
            };

            var ev15 = new CountingCircleCreated
            {
                CountingCircle = mapper.Map<CountingCircleEventData>(CcJona()),
                EventInfo = GetMockedEventInfo(360),
            };

            var ev16 = new CountingCircleCreated
            {
                CountingCircle = mapper.Map<CountingCircleEventData>(CcRapperswil()),
                EventInfo = GetMockedEventInfo(360),
            };

            var ev17 = new DomainOfInfluenceCreated
            {
                DomainOfInfluence = mapper.Map<DomainOfInfluenceEventData>(DoiRapperswilJona()),
                EventInfo = GetMockedEventInfo(360),
            };

            var ev18 = new DomainOfInfluenceCountingCircleEntriesUpdated
            {
                DomainOfInfluenceCountingCircleEntries = new DomainOfInfluenceCountingCircleEntriesEventData
                {
                    Id = DoiRapperswilJonaId,
                    CountingCircleIds =
                    {
                            CcIdRapperswil,
                            CcIdJona,
                    },
                },
                EventInfo = GetMockedEventInfo(360),
            };

            var rapperswilJonaMerger = mapper.Map<CountingCirclesMergerEventData>(CcMergeRapperswilJona());
            var ev19 = new CountingCirclesMergerScheduled
            {
                Merger = rapperswilJonaMerger,
                EventInfo = GetMockedEventInfo(390),
            };

            var ev20 = new CountingCirclesMergerActivated
            {
                Merger = rapperswilJonaMerger,
                EventInfo = GetMockedEventInfo(420),
            };

            var ev21 = new CountingCircleMerged
            {
                CountingCircleId = CcIdRapperswil,
                EventInfo = GetMockedEventInfo(420),
            };

            var ev22 = new CountingCircleMerged
            {
                CountingCircleId = CcIdJona,
                EventInfo = GetMockedEventInfo(420),
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
            await eventPublisher.Publish(9, ev9);
            await eventPublisher.Publish(10, ev10);
            await eventPublisher.Publish(11, ev11);
            await eventPublisher.Publish(12, ev12);
            await eventPublisher.Publish(13, ev13);
            await eventPublisher.Publish(14, ev14);
            await eventPublisher.Publish(15, ev15);
            await eventPublisher.Publish(16, ev16);
            await eventPublisher.Publish(17, ev17);
            await eventPublisher.Publish(18, ev18);
            await eventPublisher.Publish(19, ev19);
            await eventPublisher.Publish(20, ev20);
            await eventPublisher.Publish(21, ev21);
            await eventPublisher.Publish(22, ev22);
        });
    }

    private static EventInfo GetMockedEventInfo(long additionalDays)
    {
        return new EventInfo
        {
            Timestamp = new Timestamp
            {
                Seconds = 1594980476 + (additionalDays * 24 * 60 * 60),
            },
            Tenant = SecureConnectTestDefaults.MockedTenantDefault.ToEventInfoTenant(),
            User = SecureConnectTestDefaults.MockedUserDefault.ToEventInfoUser(),
        };
    }
}
