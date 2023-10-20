// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Events.V1.Metadata;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Jobs;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Basis.Test.Mocks;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Store;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.Testing.Utils;
using Xunit;
using Contest = Voting.Basis.Core.Domain.Contest;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.ContestTests;

public class ContestPastLockTest : BaseTest
{
    private const string ContestId = "c91b29fb-5910-42c1-9f9a-74d903b28750";

    private static readonly Guid ContestGuid = Guid.Parse(ContestId);

    private readonly TestMapper _mapper;

    private int _eventIdCounter;

    public ContestPastLockTest(TestApplicationFactory factory)
        : base(factory)
    {
        _mapper = GetService<TestMapper>();
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestProcessor()
    {
        await SeedContest(ContestId, true);
        await RunOnDb(async db =>
        {
            var contest = await db.Contests.FindAsync(ContestGuid);
            contest!.TestingPhaseEnded.Should().BeTrue();
            contest.State.Should().Be(ContestState.PastLocked);
        });
    }

    [Fact]
    public async Task JobShouldSetActiveContestToPastLocked()
    {
        await SeedContest(ContestId);

        await RunScoped<PastLockedContestJob>(job => job.Run(CancellationToken.None));
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ContestPastLocked, EventSignatureBusinessMetadata>();
        eventData.ContestId.Should().Be(ContestId);
        eventMetadata!.ContestId.Should().Be(ContestId);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestId, async () =>
        {
            await SeedContest(ContestId);
            await RunScoped<PastLockedContestJob>(job => job.Run(CancellationToken.None));
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ContestPastLocked>();
        });
    }

    [Fact]
    public async Task JobShouldSetPastUnlockedContestToPastLocked()
    {
        await SeedContest(ContestId);

        await RunScoped<IServiceProvider>(async sp =>
        {
            var pastContestJob = sp.GetRequiredService<PastLockedContestJob>();
            var aggregateRepo = sp.GetRequiredService<IAggregateRepository>();

            await ExecuteOnInfiniteValidContestKey(Guid.Parse(ContestId), sp, async () =>
            {
                await pastContestJob.Run(CancellationToken.None);

                var contestAggregate = await aggregateRepo.GetById<ContestAggregate>(Guid.Parse(ContestId));

                AdjustableMockedClock.OverrideUtcNow = MockedClock.GetDate(-2);
                contestAggregate.PastUnlock();
                AdjustableMockedClock.OverrideUtcNow = null;
                await aggregateRepo.Save(contestAggregate);
            });

            var contestPastUnlockedEvent = EventPublisherMock.GetSinglePublishedEvent<ContestPastUnlocked>();
            await TestEventPublisher.Publish(_eventIdCounter++, contestPastUnlockedEvent);

            await pastContestJob.Run(CancellationToken.None);
        });

        var eventData = EventPublisherMock.GetPublishedEvents<ContestPastLocked>();

        // should have 2 past locked events, an initial one and one after the contest got unlocked once.
        eventData.Should().HaveCount(2);
        eventData.All(c => c.ContestId == ContestId).Should().BeTrue();
    }

    private async Task SeedContest(string id, bool setPastLocked = false)
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
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
            EndOfTestingPhase = new DateTime(2010, 8, 21, 12, 45, 0, DateTimeKind.Utc).ToTimestamp(),
        };

        var contestEventData = _mapper.Map<ContestEventData>(contestProto);

        await TestEventPublisher.Publish(_eventIdCounter++, new ContestCreated
        {
            Contest = contestEventData,
        });
        await TestEventPublisher.Publish(_eventIdCounter++, new ContestTestingPhaseEnded
        {
            ContestId = id,
        });

        var domainContest = mapper.Map<Contest>(contestEventData);
        var contestAggregate = services.GetRequiredService<IAggregateFactory>().New<ContestAggregate>();

        await ExecuteOnInfiniteValidContestKey(domainContest.Id, services, async () =>
        {
            if (setPastLocked)
            {
                await TestEventPublisher.Publish(_eventIdCounter++, new ContestPastLocked
                {
                    ContestId = id,
                });
                contestAggregate.TrySetPastLocked();
            }

            AdjustableMockedClock.OverrideUtcNow = contestProto.EndOfTestingPhase.ToDateTime();
            contestAggregate.CreateFrom(domainContest);
            AdjustableMockedClock.OverrideUtcNow = null;
            contestAggregate.TryEndTestingPhase();
            await services.GetRequiredService<AggregateRepositoryMock>().Save(contestAggregate);
        });
    }
}
