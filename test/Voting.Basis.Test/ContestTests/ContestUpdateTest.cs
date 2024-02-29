// (c) Copyright 2024 by Abraxas Informatik AG
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
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.ContestTests;

public class ContestUpdateTest : BaseGrpcTest<ContestService.ContestServiceClient>
{
    public ContestUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped, false);
        await ProportionalElectionMockedData.Seed(RunScoped, false);
        await VoteMockedData.Seed(RunScoped, false);
    }

    [Fact]
    public async Task Test()
    {
        var req = NewValidRequest();
        await AdminClient.UpdateAsync(req);
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ContestUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event", d => d.Contest.Id);
        eventMetadata!.ContestId.Should().Be(req.Id);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdGossau, async () =>
        {
            await AdminClient.UpdateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ContestUpdated>();
        });
    }

    [Fact]
    public async Task TestWithEVoting()
    {
        await AdminClient.UpdateAsync(NewValidRequest(x =>
        {
            x.EVoting = true;
            x.EVotingFrom = new DateTime(2020, 8, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp();
            x.EVotingTo = new DateTime(2020, 8, 23, 22, 0, 0, DateTimeKind.Utc).ToTimestamp();
        }));

        var eventData = EventPublisherMock.GetSinglePublishedEvent<ContestUpdated>();
        eventData.MatchSnapshot("event", d => d.Contest.Id);
    }

    [Fact]
    public async Task TestWithPreviousContestShouldWork()
    {
        await AdminClient.UpdateAsync(NewValidRequest(x => x.PreviousContestId = ContestMockedData.IdPastLockedContestNoPoliticalBusinesses));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ContestUpdated>();
        eventData.MatchSnapshot("event", d => d.Contest.Id);
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = Guid.Parse(ContestMockedData.IdGossau);
        var ev = new ContestUpdated
        {
            Contest = new ContestEventData
            {
                Id = ContestMockedData.IdGossau,
                Date = new DateTime(2020, 8, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                Description = { LanguageUtil.MockAllLanguages("test") },
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                EndOfTestingPhase = new DateTime(2019, 01, 01, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            },
        };
        await TestEventPublisher.Publish(ev);
        var contest = await AdminClient.GetAsync(new GetContestRequest { Id = ContestMockedData.IdGossau });
        contest.MatchSnapshot();

        var contestEntity = await RunOnDb(async db => await db.Contests.SingleAsync(c => c.Id == id));
        contestEntity.PastLockPer.Should().Be(ev.Contest.Date.ToDateTime().NextUtcDate(true));

        await AssertHasPublishedMessage<ContestOverviewChangeMessage>(
            x => x.Contest.HasEqualIdAndNewEntityState(id, EntityState.Modified));
    }

    [Fact]
    public async Task NoDescriptionShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(o => o.Description.Clear())),
            StatusCode.InvalidArgument,
            "Description");
    }

    [Fact]
    public async Task SameEndOfTestingPhaseAsContestDateShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(o => o.EndOfTestingPhase = new DateTime(2020, 8, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp())),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task GreaterEndOfTestingPhaseThanContestDateShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(o => o.EndOfTestingPhase = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToTimestamp())),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task EndOfTestingPhaseBeforeNowShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(o =>
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
            async () => await AdminClient.UpdateAsync(NewValidRequest(o => o.EVoting = true)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task OtherTenantContestShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(
                NewValidRequest(o => o.Id = ContestMockedData.IdUzwilEvoting)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ParentContestShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(
                NewValidRequest(o => o.Id = ContestMockedData.IdBundContest)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ContestWithEndedTestingPhaseShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateAsync(
                NewValidRequest(c =>
                {
                    c.Id = ContestMockedData.IdPastLockedContestNoPoliticalBusinesses;
                    c.Date = ContestMockedData.PastLockedContestNoPoliticalBusinesses.Date.ToTimestamp();
                    c.DomainOfInfluenceId = ContestMockedData.PastLockedContestNoPoliticalBusinesses.DomainOfInfluenceId.ToString();
                    c.EndOfTestingPhase = ContestMockedData.PastLockedContestNoPoliticalBusinesses.EndOfTestingPhase.ToTimestamp();
                })),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    [Fact]
    public async Task UpdateDateToExistingContestShouldThrow()
    {
        var date = new DateTime(2020, 8, 21, 0, 0, 0, DateTimeKind.Utc);

        await RunOnDb(async db =>
        {
            db.Contests.Add(new Contest
            {
                Date = date,
                Description = LanguageUtil.MockAllLanguages("test"),
                DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGossau,
                EndOfTestingPhase = new DateTime(2020, 8, 20, 0, 0, 0, DateTimeKind.Utc),
            });
            await db.SaveChangesAsync();
        });

        await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequest(c => c.Date = date.ToTimestamp())),
            StatusCode.AlreadyExists);
    }

    [Fact]
    public async Task NotMatchingPreviousContestDoiShouldThrow()
    {
        await ModifyDbEntities<Contest>(
            c => c.Id == ContestMockedData.PastLockedContestNoPoliticalBusinesses.Id,
            c => c.DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGenf);

        await AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(x => x.PreviousContestId = ContestMockedData.IdPastLockedContestNoPoliticalBusinesses)),
            StatusCode.InvalidArgument,
            "previous contest");
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
                Date = new DateTime(2020, 8, 23, 0, 0, 0, DateTimeKind.Utc),
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
            async () => await AdminClient.UpdateAsync(NewValidRequest(x => x.Id = ContestMockedData.IdStGallenEvoting)),
            StatusCode.FailedPrecondition,
            "contest in merge set as a previous contest");
    }

    [Fact]
    public async Task EVotingAfterContestDateShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(o =>
            {
                o.EVoting = true;
                o.EVotingFrom = new DateTime(2020, 8, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp();
                o.EVotingTo = new DateTime(2020, 8, 23, 22, 0, 1, DateTimeKind.Utc).ToTimestamp();
            })),
            StatusCode.InvalidArgument,
            "E-Voting cannot take place after the contest");
    }

    [Fact]
    public async Task EndOfTestingPhaseToCloseToContestDateShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequest(x =>
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
        await ProportionalElectionUnionMockedData.Seed(RunScoped);
        await MajorityElectionUnionMockedData.Seed(RunScoped);

        var client = new ContestService.ContestServiceClient(
            CreateGrpcChannel(
                tenant: DomainOfInfluenceMockedData.Bund.SecureConnectId,
                roles: Roles.ElectionAdmin));

        var childContestDate = ContestMockedData.StGallenEvotingContest.Date;

        await client.UpdateAsync(
            NewValidRequest(o =>
            {
                o.Id = ContestMockedData.IdBundContest;
                o.Date = childContestDate.ToTimestamp();
                o.EndOfTestingPhase = childContestDate.AddDays(-1).ToTimestamp();
                o.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdBund;
            }));

        var updatedEvent = EventPublisherMock.GetSinglePublishedEvent<ContestUpdated>();
        var mergedEvent = EventPublisherMock.GetSinglePublishedEvent<ContestsMerged>();

        mergedEvent.MergedId.Should().Be(ContestMockedData.IdBundContest);
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

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new ContestService.ContestServiceClient(channel)
            .UpdateAsync(NewValidRequest());

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    private UpdateContestRequest NewValidRequest(
        Action<UpdateContestRequest>? customizer = null)
    {
        var request = new UpdateContestRequest
        {
            Id = ContestMockedData.IdGossau,
            Date = new DateTime(2020, 8, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            Description = { LanguageUtil.MockAllLanguages("test") },
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
            EndOfTestingPhase = new DateTime(2020, 8, 21, 2, 0, 0, DateTimeKind.Utc).ToTimestamp(),
        };
        customizer?.Invoke(request);
        return request;
    }
}
