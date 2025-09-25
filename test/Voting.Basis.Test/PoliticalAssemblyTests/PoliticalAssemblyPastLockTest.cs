// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Jobs;
using Voting.Basis.Test.MockedData;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Basis.Test.Mocks;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Store;
using Xunit;

namespace Voting.Basis.Test.PoliticalAssemblyTests;

public class PoliticalAssemblyPastLockTest : BaseTest
{
    private const string PoliticalAssemblyId = "c91b29fb-5910-42c1-9f9a-74d903b28750";
    private int _eventIdCounter;

    public PoliticalAssemblyPastLockTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestProcessor()
    {
        await SeedPoliticalAssembly(PoliticalAssemblyId);
        await RunOnDb(async db =>
        {
            var politicalAssembly = await db.PoliticalAssemblies.FindAsync(Guid.Parse(PoliticalAssemblyId));
            politicalAssembly!.ArchivePer.Should().BeNull();
            politicalAssembly.State.Should().Be(Data.Models.PoliticalAssemblyState.PastLocked);
        });
    }

    [Fact]
    public async Task JobShouldSetActivePoliticalAssemblytToPastLocked()
    {
        await SeedPoliticalAssembly(PoliticalAssemblyId, false);
        await RunScoped<PastLockedPoliticalAssemblyJob>(job => job.Run(CancellationToken.None));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<PoliticalAssemblyPastLocked>();
        eventData.PoliticalAssemblyId.Should().Be(PoliticalAssemblyId);
    }

    private async Task SeedPoliticalAssembly(string id, bool setPast = true, string doiId = DomainOfInfluenceMockedData.IdGossau)
    {
        using var scope = GetService<IServiceProvider>().CreateScope();
        var services = scope.ServiceProvider;
        var mapper = services.GetRequiredService<TestMapper>();
        services.GetRequiredService<IAuthStore>()
            .SetValues(string.Empty, "test", "test", []);

        var politicalAssemblyProto = new Abraxas.Voting.Basis.Services.V1.Models.PoliticalAssembly
        {
            Id = id,
            Date = new DateTime(2022, 8, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            Description = { Voting.Lib.Testing.Utils.LanguageUtil.MockAllLanguages("test1") },
            DomainOfInfluenceId = doiId,
        };

        var politicalAssemblyEventData = mapper.Map<PoliticalAssemblyEventData>(politicalAssemblyProto);
        await TestEventPublisher.Publish(_eventIdCounter++, new PoliticalAssemblyCreated
        {
            PoliticalAssembly = politicalAssemblyEventData,
        });

        var domainPoliticalAssembly = mapper.Map<Core.Domain.PoliticalAssembly>(politicalAssemblyEventData);

        var politicalAssemblyAggregate = services
            .GetRequiredService<IAggregateFactory>()
            .New<PoliticalAssemblyAggregate>();
        AdjustableMockedClock.OverrideUtcNow = new DateTime(2020, 8, 23, 0, 0, 0, DateTimeKind.Utc);
        politicalAssemblyAggregate.CreateFrom(domainPoliticalAssembly);
        AdjustableMockedClock.OverrideUtcNow = new DateTime(2022, 8, 24, 0, 0, 0, DateTimeKind.Utc);

        if (setPast)
        {
            await TestEventPublisher.Publish(_eventIdCounter++, new PoliticalAssemblyPastLocked
            {
                PoliticalAssemblyId = id,
            });
            politicalAssemblyAggregate.TrySetPastLocked();
        }

        _ = services
            .GetRequiredService<IAggregateRepository>().Save(politicalAssemblyAggregate);
    }
}
