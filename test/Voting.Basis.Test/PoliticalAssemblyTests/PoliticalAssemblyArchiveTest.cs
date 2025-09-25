// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Extensions;
using Voting.Basis.Core.Jobs;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Basis.Test.Mocks;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Store;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Mocks;
using Xunit;

namespace Voting.Basis.Test.PoliticalAssemblyTests;

public class PoliticalAssemblyArchiveTest : BaseGrpcTest<PoliticalAssemblyService.PoliticalAssemblyServiceClient>
{
    private static readonly Guid PoliticalAssemblyGuid = Guid.Parse(PoliticalAssemblyMockedData.IdPast);
    private int _eventIdCounter;

    public PoliticalAssemblyArchiveTest(TestApplicationFactory factory)
        : base(factory)
    {
        AggregateRepositoryMock.Clear();
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await PoliticalAssemblyMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task ShouldPublishEventAsElectionAdmin()
    {
        await ElectionAdminClient.ArchiveAsync(new ArchivePoliticalAssemblyRequest { Id = PoliticalAssemblyMockedData.IdPast });

        var eventData = EventPublisherMock.GetSinglePublishedEvent<PoliticalAssemblyArchived>();
        eventData.PoliticalAssemblyId.Should().Be(PoliticalAssemblyMockedData.IdPast);
    }

    [Fact]
    public async Task WithDateShouldPublishEventAsElectionAdmin()
    {
        var archivePer = MockedClock.GetTimestamp(10);
        await ElectionAdminClient.ArchiveAsync(new ArchivePoliticalAssemblyRequest
        {
            Id = PoliticalAssemblyMockedData.IdPast,
            ArchivePer = archivePer,
        });

        var eventData = EventPublisherMock.GetSinglePublishedEvent<PoliticalAssemblyArchiveDateUpdated>();
        eventData.PoliticalAssemblyId.Should().Be(PoliticalAssemblyMockedData.IdPast);
        eventData.ArchivePer.Should().Be(archivePer);
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(_eventIdCounter++, new PoliticalAssemblyArchived { PoliticalAssemblyId = PoliticalAssemblyMockedData.IdPast });

        var politicalAssembly = await RunOnDb(db => db.PoliticalAssemblies.SingleAsync(c => c.Id == PoliticalAssemblyGuid));
        politicalAssembly.State.Should().Be(PoliticalAssemblyState.Archived);
        politicalAssembly.ArchivePer.Should().Be(new DateTime(2020, 7, 17, 10, 07, 56, DateTimeKind.Utc));
    }

    [Fact]
    public async Task TestAggregateWhenArchivePerInFutureShouldUpdateArchivePer()
    {
        var archivePer = MockedClock.GetDate(hoursDelta: 10);
        await RunOnDb(async db =>
        {
            var politicalAssemblyToUpdate = await db.PoliticalAssemblies.AsTracking().SingleAsync(c => c.Id == PoliticalAssemblyGuid);
            politicalAssemblyToUpdate.ArchivePer = archivePer;
            await db.SaveChangesAsync();
        });

        await TestEventPublisher.Publish(_eventIdCounter++, new PoliticalAssemblyArchived
        {
            PoliticalAssemblyId = PoliticalAssemblyMockedData.IdPast,
            EventInfo = new EventInfo
            {
                Timestamp = MockedClock.GetTimestamp(),
                Tenant = SecureConnectTestDefaults.MockedTenantDefault.ToEventInfoTenant(),
                User = SecureConnectTestDefaults.MockedUserDefault.ToEventInfoUser(),
            },
        });

        var politicalAssembly = await RunOnDb(db => db.PoliticalAssemblies.SingleAsync(c => c.Id == PoliticalAssemblyGuid));
        politicalAssembly.State.Should().Be(PoliticalAssemblyState.Archived);
        politicalAssembly.ArchivePer.Should().Be(MockedClock.UtcNowDate);
    }

    [Fact]
    public async Task TestAggregateUpdateDate()
    {
        var archivePer = new DateTime(2010, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        await TestEventPublisher.Publish(_eventIdCounter++, new PoliticalAssemblyArchiveDateUpdated
        {
            PoliticalAssemblyId = PoliticalAssemblyMockedData.IdPast,
            ArchivePer = archivePer.ToTimestamp(),
        });

        (await RunOnDb(db => db.PoliticalAssemblies.SingleAsync(c => c.Id == PoliticalAssemblyGuid)))
            .ArchivePer
            .Should()
            .Be(archivePer);
    }

    [Fact]
    public async Task TestForeignDomainOfInfluenceShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.ArchiveAsync(new ArchivePoliticalAssemblyRequest
            {
                Id = PoliticalAssemblyMockedData.IdGenf,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task PoliticalAssemblyNotPastShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.ArchiveAsync(new ArchivePoliticalAssemblyRequest
            {
                Id = PoliticalAssemblyMockedData.IdGossau,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task WithDateInPastShouldThrow()
    {
        var archivePer = new DateTime(2019, 11, 24, 0, 0, 0, DateTimeKind.Utc).ToTimestamp();
        await AssertStatus(
            async () => await CantonAdminClient.ArchiveAsync(new ArchivePoliticalAssemblyRequest
            {
                Id = PoliticalAssemblyMockedData.IdPast,
                ArchivePer = archivePer,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task JobShouldSetPoliticalAssemblyArchived()
    {
        await TestEventPublisher.Publish(_eventIdCounter++, new PoliticalAssemblyArchiveDateUpdated
        {
            PoliticalAssemblyId = PoliticalAssemblyMockedData.IdPast,
            ArchivePer = MockedClock.GetTimestamp(-1),
        });

        await RunScoped<ArchivePoliticalAssemblyJob>(job => job.Run(CancellationToken.None));

        var eventData = EventPublisherMock.GetSinglePublishedEvent<PoliticalAssemblyArchived>();
        eventData.PoliticalAssemblyId.Should().Be(PoliticalAssemblyMockedData.IdPast);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        var politicalAssemblyId = Guid.NewGuid().ToString();
        await SeedPoliticalAssembly(politicalAssemblyId);
        await new PoliticalAssemblyService.PoliticalAssemblyServiceClient(channel)
            .ArchiveAsync(new ArchivePoliticalAssemblyRequest { Id = politicalAssemblyId });
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
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
