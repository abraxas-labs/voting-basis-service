// (c) Copyright 2022 by Abraxas Informatik AG
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
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data.Models;
using Voting.Basis.EventSignature;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.ContestTests;

public class ContestCreateTest : BaseGrpcTest<ContestService.ContestServiceClient>
{
    public ContestCreateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task Test()
    {
        var response = await AdminClient.CreateAsync(NewValidRequest());

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ContestCreated, EventSignatureBusinessMetadata>();

        eventData.Contest.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.Contest.Id);
        eventMetadata!.ContestId.Should().Be(response.Id);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        var response = await AdminClient.CreateAsync(NewValidRequest());
        var ev = EventPublisherMock.GetSinglePublishedEventWithMetadata<ContestCreated>();

        EnsureIsSignedBusinessEvent(ev, response.Id);

        // ensure that a public key signed event got emitted.
        var publicKeyCreatedEvent = EventPublisherMock.GetSinglePublishedEvent<EventSignaturePublicKeyCreated, EventSignaturePublicKeyMetadata>();
        publicKeyCreatedEvent.Data.ContestId.Should().Be(response.Id);
        publicKeyCreatedEvent.Data.PublicKey.Should().NotBeEmpty();
        publicKeyCreatedEvent.Data.KeyId.Should().NotBeEmpty();
        publicKeyCreatedEvent.Data.AuthenticationTag.Should().NotBeEmpty();
        publicKeyCreatedEvent.Metadata!.HsmSignature.Should().NotBeEmpty();

        publicKeyCreatedEvent.Data.ContestId = string.Empty;
        publicKeyCreatedEvent.Data.KeyId = string.Empty;
        publicKeyCreatedEvent.Data.PublicKey = ByteString.Empty;
        publicKeyCreatedEvent.Data.AuthenticationTag = ByteString.Empty;
        publicKeyCreatedEvent.Metadata.HsmSignature = ByteString.Empty;

        publicKeyCreatedEvent.MatchSnapshot();

        GetService<ContestCache>().Remove(Guid.Parse(response.Id));
    }

    [Fact]
    public async Task TestWithEVoting()
    {
        var response = await AdminClient.CreateAsync(NewValidRequest(x =>
        {
            x.EVoting = true;
            x.EVotingFrom = new DateTime(2020, 12, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp();
            x.EVotingTo = new DateTime(2020, 12, 23, 23, 0, 0, DateTimeKind.Utc).ToTimestamp();
        }));

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ContestCreated, EventSignatureBusinessMetadata>();

        eventData.Contest.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.Contest.Id);
        eventMetadata!.ContestId.Should().Be(response.Id);
    }

    [Fact]
    public async Task TestWithPreviousContestShouldWork()
    {
        var response = await AdminClient.CreateAsync(NewValidRequest(x => x.PreviousContestId = ContestMockedData.IdPastLockedContestNoPoliticalBusinesses));

        var eventData = EventPublisherMock.GetSinglePublishedEvent<ContestCreated>();

        eventData.Contest.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.Contest.Id);
    }

    [Fact]
    public async Task TestAggregate()
    {
        await RunOnDb(async db =>
        {
            var set = await db.Contests.ToListAsync();
            db.Contests.RemoveRange(set);
            await db.SaveChangesAsync();
        });

        var contestId1 = Guid.Parse("239702ef-3064-498c-beea-ebf57a55ff05");
        var contestId2 = Guid.Parse("d1eb8d0d-0c51-4efc-b69e-c4c68816e4b2");

        var contestEv1 = new ContestCreated
        {
            Contest = new ContestEventData
            {
                Id = contestId1.ToString(),
                Date = new DateTime(2020, 8, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                Description = { LanguageUtil.MockAllLanguages("test") },
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                EndOfTestingPhase = new DateTime(2019, 1, 1, 12, 45, 0, DateTimeKind.Utc).ToTimestamp(),
            },
        };

        var contestEv2 = new ContestCreated
        {
            Contest = new ContestEventData
            {
                Id = contestId2.ToString(),
                Date = new DateTime(2020, 8, 24, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                Description = { LanguageUtil.MockAllLanguages("test") },
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                EndOfTestingPhase = new DateTime(2019, 1, 1, 23, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                EVoting = true,
                EVotingFrom = new DateTime(2020, 01, 1, 13, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                EVotingTo = new DateTime(2020, 02, 1, 13, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            },
        };

        await TestEventPublisher.Publish(
            contestEv1,
            contestEv2);

        var contests = await AdminClient.ListSummariesAsync(new ListContestSummariesRequest());
        contests.ContestSummaries_.Should().HaveCount(2);
        contests.MatchSnapshot();

        var contestEntities = await RunOnDb(async db => await db.Contests
            .Where(c => c.Id == contestId1 || c.Id == contestId2)
            .OrderBy(c => c.Id != contestId1)
            .ToListAsync());

        contestEntities[0].PastLockPer.Should().Be(contestEv1.Contest.Date.ToDateTime().NextUtcDate(true));
        contestEntities[1].PastLockPer.Should().Be(contestEv2.Contest.Date.ToDateTime().NextUtcDate(true));

        await AssertHasPublishedMessage<ContestOverviewChangeMessage>(
            x => x.Contest.HasEqualIdAndNewEntityState(contestId1, EntityState.Added));
        await AssertHasPublishedMessage<ContestOverviewChangeMessage>(
            x => x.Contest.HasEqualIdAndNewEntityState(contestId2, EntityState.Added));
    }

    [Fact]
    public async Task NoDescriptionShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o => o.Description.Clear())),
            StatusCode.InvalidArgument,
            "Description");
    }

    [Fact]
    public async Task SameEndOfTestingPhaseAsContestDateShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o => o.EndOfTestingPhase = new DateTime(2020, 12, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp())),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task GreaterEndOfTestingPhaseThanContestDateShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o => o.EndOfTestingPhase = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToTimestamp())),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task EndOfTestingPhaseBeforeNowShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o =>
            {
                o.Date = MockedClock.GetTimestampDate(10);
                o.EndOfTestingPhase = MockedClock.GetTimestamp(-1);
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task EVotingWithoutEVotingDatesShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o => o.EVoting = true)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task EVotingAfterContestDateShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o =>
            {
                o.EVoting = true;
                o.EVotingFrom = new DateTime(2020, 12, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp();
                o.EVotingTo = new DateTime(2020, 12, 23, 23, 0, 1, DateTimeKind.Utc).ToTimestamp();
            })),
            StatusCode.InvalidArgument,
            "E-Voting cannot take place after the contest");
    }

    [Fact]
    public async Task OtherTenantDomainOfInfluenceIdShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.CreateAsync(
                NewValidRequest(o => o.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdUzwil)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ExistingContestShouldThrow()
    {
        await RunOnDb(async db =>
        {
            db.Contests.Add(new Contest
            {
                Date = new DateTime(2020, 12, 23, 0, 0, 0, DateTimeKind.Utc),
                Description = LanguageUtil.MockAllLanguages("test"),
                DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGossau,
                EndOfTestingPhase = new DateTime(2020, 12, 22, 0, 0, 0, DateTimeKind.Utc),
            });
            await db.SaveChangesAsync();
        });

        await AssertStatus(
            async () => await ElectionAdminClient.CreateAsync(NewValidRequest()),
            StatusCode.AlreadyExists);
    }

    [Fact]
    public async Task NotMatchingPreviousContestDoiShouldThrow()
    {
        await ModifyDbEntities<Contest>(
            c => c.Id == ContestMockedData.PastLockedContestNoPoliticalBusinesses.Id,
            c => c.DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGenf);

        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(x => x.PreviousContestId = ContestMockedData.IdPastLockedContestNoPoliticalBusinesses)),
            StatusCode.InvalidArgument,
            "previous contest");
    }

    [Fact]
    public async Task ExistingParentContestShouldThrow()
    {
        await RunOnDb(async db =>
        {
            db.Contests.Add(new Contest
            {
                Date = new DateTime(2020, 12, 23, 0, 0, 0, DateTimeKind.Utc),
                Description = LanguageUtil.MockAllLanguages("test"),
                DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidBund,
                EndOfTestingPhase = new DateTime(2020, 12, 22, 0, 0, 0, DateTimeKind.Utc),
            });
            await db.SaveChangesAsync();
        });

        await AssertStatus(
            async () => await ElectionAdminClient.CreateAsync(NewValidRequest()),
            StatusCode.AlreadyExists);
    }

    [Fact]
    public async Task EndOfTestingPhaseToCloseToContestDateShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.CreateAsync(NewValidRequest(x =>
            {
                x.Date = new DateTime(2022, 12, 22, 0, 0, 0, DateTimeKind.Utc).ToTimestamp();
                x.EndOfTestingPhase = new DateTime(2022, 12, 17, 23, 59, 0, DateTimeKind.Utc).ToTimestamp();
            })),
            StatusCode.InvalidArgument,
            "The testing phase must end at the earliest 4.00:00:00 before the contest date");
    }

    [Fact]
    public async Task TestMerge()
    {
        await VoteMockedData.Seed(RunScoped, false);
        await ProportionalElectionMockedData.Seed(RunScoped, false);
        await MajorityElectionMockedData.Seed(RunScoped, false);
        await ProportionalElectionUnionMockedData.Seed(RunScoped);
        await MajorityElectionUnionMockedData.Seed(RunScoped);

        var client = new ContestService.ContestServiceClient(
            CreateGrpcChannel(
                tenant: DomainOfInfluenceMockedData.Bund.SecureConnectId,
                roles: Roles.ElectionAdmin));

        var childContestDate = ContestMockedData.StGallenEvotingContest.Date;

        await client.CreateAsync(
            NewValidRequest(o =>
            {
                o.Date = childContestDate.ToTimestamp();
                o.EndOfTestingPhase = childContestDate.AddDays(-1).ToTimestamp();
                o.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdBund;
            }));

        var createdEvent = EventPublisherMock.GetSinglePublishedEvent<ContestCreated>();
        var mergedEvent = EventPublisherMock.GetSinglePublishedEvent<ContestsMerged>();

        mergedEvent.MergedId.Should().NotBeEmpty();
        mergedEvent.OldIds.Should().Contain(ContestMockedData.IdStGallenEvoting);
        var newContestId = mergedEvent.MergedId;

        var voteMovedEvents = EventPublisherMock.GetPublishedEvents<VoteToNewContestMoved, EventSignatureBusinessMetadata>();
        var peMovedEvents = EventPublisherMock.GetPublishedEvents<ProportionalElectionToNewContestMoved, EventSignatureBusinessMetadata>();
        var meMovedEvents = EventPublisherMock.GetPublishedEvents<MajorityElectionToNewContestMoved, EventSignatureBusinessMetadata>();
        var peuMovedEvents = EventPublisherMock.GetPublishedEvents<ProportionalElectionUnionToNewContestMoved, EventSignatureBusinessMetadata>();
        var meuMovedEvents = EventPublisherMock.GetPublishedEvents<MajorityElectionUnionToNewContestMoved, EventSignatureBusinessMetadata>();

        voteMovedEvents.Any().Should().BeTrue();
        peMovedEvents.Any().Should().BeTrue();
        meMovedEvents.Any().Should().BeTrue();
        peuMovedEvents.Any().Should().BeTrue();
        meuMovedEvents.Any().Should().BeTrue();

        voteMovedEvents.Select(x => (x.Data.NewContestId, x.Metadata!.ContestId))
            .Concat(peMovedEvents.Select(x => (x.Data.NewContestId, x.Metadata!.ContestId)))
            .Concat(meMovedEvents.Select(x => (x.Data.NewContestId, x.Metadata!.ContestId)))
            .Concat(peuMovedEvents.Select(x => (x.Data.NewContestId, x.Metadata!.ContestId)))
            .Concat(meuMovedEvents.Select(x => (x.Data.NewContestId, x.Metadata!.ContestId)))
            .All(x => x == (newContestId, newContestId))
            .Should().BeTrue();
    }

    [Fact]
    public async Task TestMergeShouldTriggerEventSignatureAndSignEvent()
    {
        var contestCache = GetService<ContestCache>();

        await VoteMockedData.Seed(RunScoped, false);
        await ProportionalElectionMockedData.Seed(RunScoped, false);
        await MajorityElectionMockedData.Seed(RunScoped, false);
        await ProportionalElectionUnionMockedData.Seed(RunScoped);
        await MajorityElectionUnionMockedData.Seed(RunScoped);

        foreach (var contestCacheEntry in contestCache.GetAll())
        {
            contestCache.Remove(contestCacheEntry.Id);
        }

        var client = new ContestService.ContestServiceClient(
            CreateGrpcChannel(
                tenant: DomainOfInfluenceMockedData.Bund.SecureConnectId,
                roles: Roles.ElectionAdmin));

        var childContestDate = ContestMockedData.StGallenEvotingContest.Date;

        await client.CreateAsync(
            NewValidRequest(o =>
            {
                o.Date = childContestDate.ToTimestamp();
                o.EndOfTestingPhase = childContestDate.AddDays(-1).ToTimestamp();
                o.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdBund;
            }));

        var createdEvent = EventPublisherMock.GetSinglePublishedEventWithMetadata<ContestCreated>();
        var mergedEvent = EventPublisherMock.GetSinglePublishedEventWithMetadata<ContestsMerged>();
        var newContestId = (mergedEvent.Data as ContestsMerged)!.MergedId;
        var oldContestId = ContestMockedData.IdStGallenEvoting;

        EnsureIsSignedBusinessEvent(createdEvent, newContestId);
        EnsureIsSignedBusinessEvent(mergedEvent, newContestId);

        var voteMovedEvents = EventPublisherMock.GetPublishedEventsWithMetadata<VoteToNewContestMoved>();
        var peMovedEvents = EventPublisherMock.GetPublishedEventsWithMetadata<ProportionalElectionToNewContestMoved>();
        var meMovedEvents = EventPublisherMock.GetPublishedEventsWithMetadata<MajorityElectionToNewContestMoved>();
        var peuMovedEvents = EventPublisherMock.GetPublishedEventsWithMetadata<ProportionalElectionUnionToNewContestMoved>();
        var meuMovedEvents = EventPublisherMock.GetPublishedEventsWithMetadata<MajorityElectionUnionToNewContestMoved>();

        var contestRelatedEvents = voteMovedEvents
            .Concat(peMovedEvents)
            .Concat(meMovedEvents)
            .Concat(peuMovedEvents)
            .Concat(meuMovedEvents)
            .ToList();

        var contestDeletedEvents = EventPublisherMock.GetPublishedEventsWithMetadata<ContestDeleted>();

        foreach (var contestRelatedEvent in contestRelatedEvents)
        {
            EnsureIsSignedBusinessEvent(contestRelatedEvent, newContestId);
        }

        var publicKeyCreatedEvents = EventPublisherMock.GetPublishedEvents<EventSignaturePublicKeyCreated>();
        publicKeyCreatedEvents.Count().Should().Be(2);

        var oldContestPublicKeySignedEvent = publicKeyCreatedEvents.FirstOrDefault(x => x.ContestId == oldContestId);
        oldContestPublicKeySignedEvent.Should().NotBeNull();

        var newContestPublicKeySignedEvent = publicKeyCreatedEvents.FirstOrDefault(x => x.ContestId == newContestId);
        newContestPublicKeySignedEvent.Should().NotBeNull();
    }

    [Fact]
    public async Task ExistingChildWhichIsAlreadyPreviousContestShouldThrow()
    {
        await RunOnDb(async db =>
        {
            var id = Guid.Parse("4162e837-f8a1-4dcb-8794-d21f07411e21");
            db.Contests.Add(new Contest
            {
                Id = id,
                Date = new DateTime(2020, 12, 23, 0, 0, 0, DateTimeKind.Utc),
                Description = LanguageUtil.MockAllLanguages("test"),
                DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGossau,
                EndOfTestingPhase = new DateTime(2020, 12, 22, 0, 0, 0, DateTimeKind.Utc),
                State = ContestState.TestingPhase,
            });

            var bundContest = await db.Contests.AsTracking().SingleAsync(x => x.Id == ContestMockedData.BundContest.Id);
            bundContest.PreviousContestId = id;
            await db.SaveChangesAsync();
        });

        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(x => x.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen)),
            StatusCode.FailedPrecondition,
            "contest in merge set as a previous contest");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new ContestService.ContestServiceClient(channel)
            .CreateAsync(NewValidRequest());

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    private CreateContestRequest NewValidRequest(
        Action<CreateContestRequest>? customizer = null)
    {
        var request = new CreateContestRequest
        {
            Date = new DateTime(2020, 12, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            Description = { LanguageUtil.MockAllLanguages("test") },
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
            EndOfTestingPhase = new DateTime(2020, 12, 22, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
        };
        customizer?.Invoke(request);
        return request;
    }
}
