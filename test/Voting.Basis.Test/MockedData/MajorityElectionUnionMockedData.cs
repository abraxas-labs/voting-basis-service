// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Store;
using Voting.Lib.Iam.Testing.AuthenticationScheme;

namespace Voting.Basis.Test.MockedData;

public static class MajorityElectionUnionMockedData
{
    public const string IdBund = "04047657-8204-4baa-91be-45c0bb63d8d1";
    public const string IdBundDifferentTenant = "02ac19a5-4253-424a-9ae2-c7bdf8ceceaf";
    public const string IdStGallen1 = "e73fa1c9-3532-4076-b3fc-8b7bd2a915ac";
    public const string IdStGallen2 = "d9196504-8c61-4568-94f4-9b8aec9124fe";
    public const string IdStGallenDifferentTenant = "ae335b68-1977-4459-a854-9718a6d8e276";
    public const string IdKirche = "1702a77c-f7c7-44a3-a097-039733bc136f";

    public static MajorityElectionUnion BundUnion
        => new MajorityElectionUnion
        {
            Id = Guid.Parse(IdBund),
            Description = "Bund Union",
            ContestId = ContestMockedData.BundContest.Id,
            SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
            MajorityElectionUnionEntries =
            {
                    new MajorityElectionUnionEntry
                    {
                        Id = Guid.Parse("16f123f4-7cc8-4cc6-a55d-3999b56692cc"),
                        MajorityElectionId = MajorityElectionMockedData.StGallenMajorityElectionInContestBund.Id,
                    },
            },
        };

    public static MajorityElectionUnion BundUnionDifferentTenant
        => new MajorityElectionUnion
        {
            Id = Guid.Parse(IdBundDifferentTenant),
            Description = "Bund Union different Tenant",
            ContestId = ContestMockedData.BundContest.Id,
            SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
            MajorityElectionUnionEntries =
            {
                    new MajorityElectionUnionEntry
                    {
                        Id = Guid.Parse("5c789592-8f7f-4e49-a9d5-50f7f35a7815"),
                        MajorityElectionId = MajorityElectionMockedData.UzwilMajorityElectionInContestBundWithoutChilds.Id,
                    },
            },
        };

    public static MajorityElectionUnion StGallen1
        => new MajorityElectionUnion
        {
            Id = Guid.Parse(IdStGallen1),
            Description = "St. Gallen 1",
            ContestId = ContestMockedData.StGallenEvotingContest.Id,
            SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
            MajorityElectionUnionEntries =
            {
                    new MajorityElectionUnionEntry
                    {
                        Id = Guid.Parse("deef10e6-c8b5-4baa-891a-48711d8117d2"),
                        MajorityElectionId = MajorityElectionMockedData.GossauMajorityElectionInContestStGallen.Id,
                    },
                    new MajorityElectionUnionEntry
                    {
                        Id = Guid.Parse("6b8722b8-91ac-44e6-8db4-e2b6345a1f46"),
                        MajorityElectionId = MajorityElectionMockedData.StGallenMajorityElectionInContestStGallen.Id,
                    },
            },
        };

    public static MajorityElectionUnion StGallen2
        => new MajorityElectionUnion
        {
            Id = Guid.Parse(IdStGallen2),
            Description = "St. Gallen 2",
            ContestId = ContestMockedData.StGallenEvotingContest.Id,
            SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
        };

    public static MajorityElectionUnion StGallenDifferentTenant
        => new MajorityElectionUnion
        {
            Id = Guid.Parse(IdStGallenDifferentTenant),
            Description = "St. Gallen different Tenant",
            ContestId = ContestMockedData.StGallenEvotingContest.Id,
            SecureConnectId = SecureConnectTestDefaults.MockedTenantStGallen.Id,
        };

    public static MajorityElectionUnion Kirche
        => new MajorityElectionUnion
        {
            Id = Guid.Parse(IdKirche),
            Description = "Kirche",
            ContestId = ContestMockedData.KirchenContest.Id,
            SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
        };

    public static IEnumerable<MajorityElectionUnion> All
    {
        get
        {
            yield return BundUnion;
            yield return BundUnionDifferentTenant;
            yield return StGallen1;
            yield return StGallen2;
            yield return StGallenDifferentTenant;
            yield return Kirche;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            db.MajorityElectionUnions.AddRange(All);
            await db.SaveChangesAsync();

            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues("test", "test", Enumerable.Empty<string>());

            var aggregateRepository = sp.GetRequiredService<IAggregateRepository>();
            var aggregateFactory = sp.GetRequiredService<IAggregateFactory>();
            var mapper = sp.GetRequiredService<TestMapper>();

            var unions = All.Select(v => ToAggregate(v, aggregateFactory, mapper));
            foreach (var union in unions)
            {
                await aggregateRepository.Save(union);
            }

            sp.GetRequiredService<EventPublisherMock>().Clear();
        });
    }

    private static MajorityElectionUnionAggregate ToAggregate(
        MajorityElectionUnion union,
        IAggregateFactory aggregateFactory,
        TestMapper mapper)
    {
        var aggregate = aggregateFactory.New<MajorityElectionUnionAggregate>();
        var domainUnion = mapper.Map<Core.Domain.MajorityElectionUnion>(union);
        aggregate.CreateFrom(domainUnion);
        return aggregate;
    }
}
