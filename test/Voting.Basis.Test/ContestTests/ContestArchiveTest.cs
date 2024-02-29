// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Events.V1.Metadata;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Snapper;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.EventSignature;
using Voting.Basis.Core.Extensions;
using Voting.Basis.Core.Jobs;
using Voting.Basis.Data.Models;
using Voting.Basis.EventSignature;
using Voting.Basis.Test.MockedData;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Basis.Test.Mocks;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Store;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.Testing.Utils;
using Xunit;
using Contest = Voting.Basis.Core.Domain.Contest;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.ContestTests;

public class ContestArchiveTest : BaseGrpcTest<ContestService.ContestServiceClient>
{
    private const string ContestId = "ab896b48-36a3-4b89-90c3-190e4ff471e4";

    private static readonly Guid ContestGuid = Guid.Parse(ContestId);

    private int _eventIdCounter;

    public ContestArchiveTest(TestApplicationFactory factory)
        : base(factory)
    {
        AggregateRepositoryMock.Clear();
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
        await SeedContest(ContestId);
    }

    [Fact]
    public async Task ShouldPublishEvent()
    {
        await AdminClient.ArchiveAsync(new ArchiveContestRequest { Id = ContestId });

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ContestArchived, EventSignatureBusinessMetadata>();
        eventData.ContestId.Should().Be(ContestId);
        eventData.ShouldMatchSnapshot();
        eventMetadata!.ContestId.Should().Be(ContestId);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestId, async () =>
        {
            await AdminClient.ArchiveAsync(new ArchiveContestRequest { Id = ContestId });
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ContestArchived>();
        });
    }

    [Fact]
    public async Task WithDateShouldPublishEvent()
    {
        var archivePer = MockedClock.GetTimestamp(10);
        await AdminClient.ArchiveAsync(new ArchiveContestRequest
        {
            Id = ContestId,
            ArchivePer = archivePer,
        });

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ContestArchiveDateUpdated, EventSignatureBusinessMetadata>();
        eventData.ContestId.Should().Be(ContestId);
        eventData.ArchivePer.Should().Be(archivePer);
        eventMetadata!.ContestId.Should().Be(ContestId);
    }

    [Fact]
    public async Task ShouldPublishEventAsElectionAdmin()
    {
        await ElectionAdminClient.ArchiveAsync(new ArchiveContestRequest { Id = ContestId });

        var eventData = EventPublisherMock.GetSinglePublishedEvent<ContestArchived>();
        eventData.ContestId.Should().Be(ContestId);
    }

    [Fact]
    public async Task WithDateShouldPublishEventAsElectionAdmin()
    {
        var archivePer = MockedClock.GetTimestamp(10);
        await ElectionAdminClient.ArchiveAsync(new ArchiveContestRequest
        {
            Id = ContestId,
            ArchivePer = archivePer,
        });

        var eventData = EventPublisherMock.GetSinglePublishedEvent<ContestArchiveDateUpdated>();
        eventData.ContestId.Should().Be(ContestId);
        eventData.ArchivePer.Should().Be(archivePer);
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(_eventIdCounter++, new ContestArchived { ContestId = ContestId });

        var contest = await RunOnDb(db => db.Contests.SingleAsync(c => c.Id == ContestGuid));
        contest.State.Should().Be(ContestState.Archived);
        contest.ArchivePer.Should().Be(new DateTime(2020, 7, 17, 10, 07, 56, DateTimeKind.Utc));
    }

    [Fact]
    public async Task TestAggregateWhenArchivePerInFutureShouldUpdateArchivePer()
    {
        var archivePer = MockedClock.GetDate(hoursDelta: 10);
        await RunOnDb(async db =>
        {
            var contestToUpdate = await db.Contests.AsTracking().SingleAsync(c => c.Id == ContestGuid);
            contestToUpdate.ArchivePer = archivePer;
            await db.SaveChangesAsync();
        });

        await TestEventPublisher.Publish(_eventIdCounter++, new ContestArchived
        {
            ContestId = ContestId,
            EventInfo = new EventInfo
            {
                Timestamp = MockedClock.GetTimestamp(),
                Tenant = SecureConnectTestDefaults.MockedTenantDefault.ToEventInfoTenant(),
                User = SecureConnectTestDefaults.MockedUserDefault.ToEventInfoUser(),
            },
        });

        var contest = await RunOnDb(db => db.Contests.SingleAsync(c => c.Id == ContestGuid));
        contest.State.Should().Be(ContestState.Archived);
        contest.ArchivePer.Should().Be(MockedClock.UtcNowDate);
    }

    [Fact]
    public async Task TestAggregateUpdateDate()
    {
        var archivePer = new DateTime(2010, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        await TestEventPublisher.Publish(_eventIdCounter++, new ContestArchiveDateUpdated
        {
            ContestId = ContestId,
            ArchivePer = archivePer.ToTimestamp(),
        });

        (await RunOnDb(db => db.Contests.SingleAsync(c => c.Id == ContestGuid)))
            .ArchivePer
            .Should()
            .Be(archivePer);
    }

    [Fact]
    public async Task TestForeignDomainOfInfluenceShouldThrow()
    {
        var id = "98952b97-5cc9-4d0f-a321-57900efb5d2c";
        await SeedContest(id, true, DomainOfInfluenceMockedData.IdGenf);
        await AssertStatus(
            async () => await ElectionAdminClient.ArchiveAsync(new ArchiveContestRequest
            {
                Id = id,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ContestNotPastShouldThrow()
    {
        var id = "c90795b7-b535-4a70-b7da-482b6e1f7a08";
        await SeedContest(id, false);
        await AssertStatus(
            async () => await ElectionAdminClient.ArchiveAsync(new ArchiveContestRequest
            {
                Id = id,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task WithDateInPastShouldThrow()
    {
        var archivePer = MockedClock.GetTimestamp(-10);
        await AssertStatus(
            async () => await AdminClient.ArchiveAsync(new ArchiveContestRequest
            {
                Id = ContestId,
                ArchivePer = archivePer,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task JobShouldSetContestArchived()
    {
        await TestEventPublisher.Publish(_eventIdCounter++, new ContestArchiveDateUpdated
        {
            ContestId = ContestId,
            ArchivePer = MockedClock.GetTimestamp(-1),
        });

        await RunScoped<ArchiveContestJob>(job => job.Run(CancellationToken.None));

        var eventData = EventPublisherMock.GetSinglePublishedEvent<ContestArchived>();
        eventData.ContestId.Should().Be(ContestId);
    }

    [Fact]
    public async Task TestTransientCatchUpInReplay()
    {
        var contestId = Guid.Parse("be5eaf43-9f4e-4749-8a3f-c90b692d8ebc");
        await SeedContest(contestId.ToString(), false);

        var testEventPublisher = GetService<TestEventPublisher>();
        var contestCache = GetService<ContestCache>();

        contestCache.Add(new() { Id = contestId });

        await testEventPublisher.Publish(
            true,
            new ContestArchived
            {
                EventInfo = GetMockedEventInfo(),
                ContestId = contestId.ToString(),
            });

        contestCache.Get(contestId).Should().BeNull();
        EventPublisherMock.GetPublishedEvents<EventSignaturePublicKeyDeleted>().Should().BeEmpty();
    }

    [Fact]
    public async Task TestTransientCatchUpInLiveProcessingWithExistingKey()
    {
        var contestId = Guid.Parse("be5eaf43-9f4e-4749-8a3f-c90b692d8ebc");
        await SeedContest(contestId.ToString(), false);

        var testEventPublisher = GetService<TestEventPublisher>();
        var contestCache = GetService<ContestCache>();

        // Make sure that an active key exists. This also ensures that the correct aggregate exists
        var signatureService = GetService<EventSignatureService>();
        await signatureService.EnsureActiveSignature(contestId, MockedClock.UtcNowDate);

        var cacheEntry = contestCache.Get(contestId);
        cacheEntry.Should().NotBeNull();
        var keyId = cacheEntry!.KeyData!.Key.Id;

        await testEventPublisher.Publish(
            false,
            new ContestArchived
            {
                EventInfo = GetMockedEventInfo(),
                ContestId = contestId.ToString(),
            });

        contestCache.Get(contestId).Should().BeNull();
        var ev = EventPublisherMock.GetSinglePublishedEvent<EventSignaturePublicKeyDeleted, EventSignaturePublicKeyMetadata>();
        ev.Data.KeyId.Should().Be(keyId);
        ev.Data.AuthenticationTag.Should().NotBeEmpty();
        ev.Metadata!.HsmSignature.Should().NotBeEmpty();

        ev.Data.KeyId = string.Empty;
        ev.Data.AuthenticationTag = ByteString.Empty;
        ev.Metadata.HsmSignature = ByteString.Empty;
        ev.MatchSnapshot();
    }

    [Fact]
    public async Task TestTransientCatchUpInLiveProcessingWithoutKey()
    {
        var contestId = Guid.Parse("be5eaf43-9f4e-4749-8a3f-c90b692d8ebc");
        await SeedContest(contestId.ToString(), false);

        var testEventPublisher = GetService<TestEventPublisher>();
        var contestCache = GetService<ContestCache>();

        contestCache.Add(new() { Id = contestId });

        await testEventPublisher.Publish(
            false,
            new ContestArchived
            {
                EventInfo = GetMockedEventInfo(),
                ContestId = contestId.ToString(),
            });

        contestCache.Get(contestId).Should().BeNull();
        EventPublisherMock.GetPublishedEvents<EventSignaturePublicKeyCreated>().Should().BeEmpty();
        EventPublisherMock.GetPublishedEvents<EventSignaturePublicKeyDeleted>().Should().BeEmpty();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ContestService.ContestServiceClient(channel)
            .ArchiveAsync(new ArchiveContestRequest { Id = ContestId });
    }

    private async Task SeedContest(string id, bool setPast = true, string doiId = DomainOfInfluenceMockedData.IdGossau)
    {
        using var scope = GetService<IServiceProvider>().CreateScope();
        var services = scope.ServiceProvider;
        var mapper = services.GetRequiredService<TestMapper>();
        services.GetRequiredService<IAuthStore>()
            .SetValues(string.Empty, "test", "test", Enumerable.Empty<string>());

        var contestProto = new ProtoModels.Contest
        {
            Id = id,
            Date = new DateTime(2010, 8, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            Description = { LanguageUtil.MockAllLanguages("test1") },
            DomainOfInfluenceId = doiId,
            EndOfTestingPhase = new DateTime(2010, 8, 20, 12, 45, 0, DateTimeKind.Utc).ToTimestamp(),
        };

        var contestEventData = mapper.Map<ContestEventData>(contestProto);
        await TestEventPublisher.Publish(_eventIdCounter++, new ContestCreated
        {
            Contest = contestEventData,
        });
        await TestEventPublisher.Publish(_eventIdCounter++, new ContestTestingPhaseEnded
        {
            ContestId = id,
        });

        var domainContest = mapper.Map<Contest>(contestEventData);

        var contestAggregate = services
            .GetRequiredService<IAggregateFactory>()
            .New<ContestAggregate>();
        AdjustableMockedClock.OverrideUtcNow = contestProto.EndOfTestingPhase.ToDateTime();
        contestAggregate.CreateFrom(domainContest);
        AdjustableMockedClock.OverrideUtcNow = null;
        contestAggregate.TryEndTestingPhase();

        if (setPast)
        {
            await TestEventPublisher.Publish(_eventIdCounter++, new ContestPastLocked
            {
                ContestId = id,
            });
            contestAggregate.TrySetPastLocked();
        }

        await ExecuteOnInfiniteValidContestKey(Guid.Parse(id), services, () => services
            .GetRequiredService<IAggregateRepository>()
            .Save(contestAggregate));
    }
}
