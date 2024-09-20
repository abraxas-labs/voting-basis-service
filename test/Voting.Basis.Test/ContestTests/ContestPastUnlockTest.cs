// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Events.V1.Metadata;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Snapper;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Basis.Test.Mocks;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Store;
using Voting.Lib.Testing.Utils;
using Xunit;
using Contest = Voting.Basis.Core.Domain.Contest;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.ContestTests;

public class ContestPastUnlockTest : BaseGrpcTest<ContestService.ContestServiceClient>
{
    private const string ContestId = "c91b29fb-5910-42c1-9f9a-74d903b28750";
    private static readonly Guid ContestGuid = Guid.Parse(ContestId);

    private int _eventIdCounter;

    public ContestPastUnlockTest(TestApplicationFactory factory)
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
        await SeedContest(ContestId);

        var ev = new ContestPastUnlocked
        {
            ContestId = ContestId,
        };

        await TestEventPublisher.Publish(
        _eventIdCounter++,
        ev);

        await RunOnDb(async db =>
        {
            var contest = await db.Contests.FindAsync(ContestGuid);
            contest!.TestingPhaseEnded.Should().BeTrue();
            contest.State.Should().Be(ContestState.PastUnlocked);
            contest.PastLockPer.Should().Be(ev.EventInfo.Timestamp.ToDateTime().NextUtcDate(true));
        });
    }

    [Fact]
    public async Task ShouldWorkAsAdmin()
    {
        await SeedContest(ContestId);
        await AdminClient.PastUnlockAsync(new PastUnlockContestRequest { Id = ContestId });

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ContestPastUnlocked, EventSignatureBusinessMetadata>();
        eventData.ContestId.Should().Be(ContestId);
        eventData.ShouldMatchSnapshot();
        eventMetadata!.ContestId.Should().Be(ContestId);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestId, async () =>
        {
            await SeedContest(ContestId);
            await AdminClient.PastUnlockAsync(new PastUnlockContestRequest { Id = ContestId });
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ContestPastUnlocked>();
        });
    }

    [Fact]
    public async Task ShouldWorkAsElectionAdmin()
    {
        await SeedContest(ContestId);
        await ElectionAdminClient.PastUnlockAsync(new PastUnlockContestRequest { Id = ContestId });

        var eventData = EventPublisherMock.GetSinglePublishedEvent<ContestPastUnlocked>();
        eventData.ContestId.Should().Be(ContestId);
        eventData.ShouldMatchSnapshot();
    }

    [Fact]
    public async Task ShouldWorkAsNonOwnerButWithReadPermissions()
    {
        await SeedContest(ContestId, doiId: DomainOfInfluenceMockedData.IdUzwil);
        await ElectionAdminClient.PastUnlockAsync(new PastUnlockContestRequest { Id = ContestId });

        var eventData = EventPublisherMock.GetSinglePublishedEvent<ContestPastUnlocked>();
        eventData.ContestId.Should().Be(ContestId);
        eventData.ShouldMatchSnapshot();
    }

    [Fact]
    public async Task ShouldThrowAsNonOwnerWithNoReadPermissions()
    {
        await SeedContest(ContestId, doiId: DomainOfInfluenceMockedData.IdGenf);
        await AssertStatus(
            async () => await ElectionAdminClient.PastUnlockAsync(new PastUnlockContestRequest
            {
                Id = ContestId,
            }),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task ContestActiveShouldThrow()
    {
        var id = "c90795b7-b535-4a70-b7da-482b6e1f7a08";
        await SeedContest(id, setPast: false);
        await AssertStatus(
            async () => await ElectionAdminClient.PastUnlockAsync(new PastUnlockContestRequest
            {
                Id = id,
            }),
            StatusCode.InvalidArgument,
            "a contest can only be unlocked if it is in the past locked state");
    }

    [Fact]
    public async Task ContestArchivedShouldThrow()
    {
        var id = "c90795b7-b535-4a70-b7da-482b6e1f7a08";
        await SeedContest(id, setArchived: true);
        await AssertStatus(
            async () => await ElectionAdminClient.PastUnlockAsync(new PastUnlockContestRequest
            {
                Id = id,
            }),
            StatusCode.InvalidArgument,
            "a contest can only be unlocked if it is in the past locked state");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        var contestId = Guid.NewGuid().ToString();
        await SeedContest(contestId);
        await new ContestService.ContestServiceClient(channel)
            .PastUnlockAsync(new PastUnlockContestRequest { Id = contestId });
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
    }

    private async Task SeedContest(string id, bool setPast = true, bool setArchived = false, string doiId = DomainOfInfluenceMockedData.IdGossau)
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
            EndOfTestingPhase = new DateTime(2010, 8, 22, 12, 45, 0, DateTimeKind.Utc).ToTimestamp(),
        };

        var contestEventData = services.GetRequiredService<TestMapper>().Map<ContestEventData>(contestProto);

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

        if (setArchived)
        {
            await TestEventPublisher.Publish(_eventIdCounter++, new ContestArchived
            {
                ContestId = id,
            });
            contestAggregate.TryArchive();
        }

        await ExecuteOnInfiniteValidContestKey(domainContest.Id, services, () => services.GetRequiredService<AggregateRepositoryMock>().Save(contestAggregate));
    }
}
