// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Basis.Test.Mocks;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Store;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.Testing.Utils;

namespace Voting.Basis.Test.MockedData;

public static class PoliticalAssemblyMockedData
{
    public const string IdGossau = "b629dad9-afce-44cf-bcbb-22516ba86007";
    public const string IdKirche = "94c8723e-015d-455d-a5b9-705320a3edd1";
    public const string IdPast = "40869f32-98f1-42cb-b3f1-7db4b138f4b6";

    public static PoliticalAssembly PastPoliticalAssembly
        => new PoliticalAssembly
        {
            Id = Guid.Parse(IdPast),
            Date = new DateTime(2019, 11, 24, 0, 0, 0, DateTimeKind.Utc),
            Description = LanguageUtil.MockAllLanguages("Vergangene Versammlung"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGossau,
        };

    public static PoliticalAssembly GossauPoliticalAssembly
        => new PoliticalAssembly
        {
            Id = Guid.Parse(IdGossau),
            Date = new DateTime(2020, 2, 29, 0, 0, 0, DateTimeKind.Utc),
            Description = LanguageUtil.MockAllLanguages("Gossau Versammlung"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGossau,
        };

    public static PoliticalAssembly KirchenPoliticalAssembly
        => new PoliticalAssembly
        {
            Id = Guid.Parse(IdKirche),
            Date = new DateTime(2020, 2, 29, 0, 0, 0, DateTimeKind.Utc),
            Description = LanguageUtil.MockAllLanguages("Kirche Versammlung"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidKirchgemeinde,
        };

    public static IEnumerable<PoliticalAssembly> All
    {
        get
        {
            yield return PastPoliticalAssembly;
            yield return GossauPoliticalAssembly;
            yield return KirchenPoliticalAssembly;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await DomainOfInfluenceMockedData.Seed(runScoped);

        var all = All.ToList();

        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();

            db.PoliticalAssemblies.AddRange(all);
            await db.SaveChangesAsync();

            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues(string.Empty, "test", "test", Enumerable.Empty<string>());

            var aggregateRepository = sp.GetRequiredService<IAggregateRepository>();
            var aggregateFactory = sp.GetRequiredService<IAggregateFactory>();
            var mapper = sp.GetRequiredService<TestMapper>();

            var politicalAssemblyAggregates = All.Select(x => ToAggregate(x, aggregateFactory, mapper));

            foreach (var politicalAssembly in politicalAssemblyAggregates)
            {
                await aggregateRepository.Save(politicalAssembly);
            }

            sp.GetRequiredService<EventPublisherMock>().Clear();
        });
    }

    public static PoliticalAssemblyAggregate ToAggregate(PoliticalAssembly politicalAssembly, IAggregateFactory aggregateFactory, TestMapper mapper)
    {
        var aggregate = aggregateFactory.New<PoliticalAssemblyAggregate>();
        var p = mapper.Map<Core.Domain.PoliticalAssembly>(politicalAssembly);

        // since only date > now is allowed,
        // we need to set the system time for the create method accordingly.
        if (p.Date < MockedClock.UtcNowDate)
        {
            AdjustableMockedClock.OverrideUtcNow = p.Date;
        }

        aggregate.CreateFrom(p);
        AdjustableMockedClock.OverrideUtcNow = null;
        return aggregate;
    }
}
