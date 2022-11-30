// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Events.V1.Metadata;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.VoteTests;

public class VoteCreateTest : BaseGrpcTest<VoteService.VoteServiceClient>
{
    public VoteCreateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await VoteMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task Test()
    {
        var response = await AdminClient.CreateAsync(NewValidRequest());

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<VoteCreated, EventSignatureBusinessMetadata>();

        eventData.Vote.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.Vote.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.CreateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteCreated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id1 = Guid.Parse("5483076b-e596-44d3-b34e-6e9220eed84c");
        var id2 = Guid.Parse("051c2a1a-9df6-4c9c-98a2-d7f3d720c62e");

        await TestEventPublisher.Publish(
            new VoteCreated
            {
                Vote = new VoteEventData
                {
                    Id = id1.ToString(),
                    PoliticalBusinessNumber = "2000",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Abstimmung 1") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Neue 1") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                    ContestId = ContestMockedData.IdGossau,
                    ResultEntry = SharedProto.VoteResultEntry.Detailed,
                    AutomaticBallotBundleNumberGeneration = true,
                    BallotBundleSampleSizePercent = 10,
                    EnforceResultEntryForCountingCircles = true,
                    ReviewProcedure = SharedProto.VoteReviewProcedure.Physically,
                    EnforceReviewProcedureForCountingCircles = true,
                },
            },
            new VoteCreated
            {
                Vote = new VoteEventData
                {
                    Id = id2.ToString(),
                    PoliticalBusinessNumber = "2001",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Abstimmung 2") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Neue 2") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                    ContestId = ContestMockedData.IdBundContest,
                    ResultAlgorithm = SharedProto.VoteResultAlgorithm.CountingCircleUnanimity,
                    ResultEntry = SharedProto.VoteResultEntry.FinalResults,
                    AutomaticBallotBundleNumberGeneration = false,
                    BallotBundleSampleSizePercent = 0,
                    EnforceResultEntryForCountingCircles = false,
                    ReviewProcedure = SharedProto.VoteReviewProcedure.Electronically,
                    EnforceReviewProcedureForCountingCircles = false,
                },
            });

        var vote1 = await AdminClient.GetAsync(new GetVoteRequest
        {
            Id = id1.ToString(),
        });
        var vote2 = await AdminClient.GetAsync(new GetVoteRequest
        {
            Id = id2.ToString(),
        });
        vote1.MatchSnapshot("1");
        vote2.MatchSnapshot("2");

        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusiness.HasEqualIdAndNewEntityState(id1, EntityState.Added));
        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusiness.HasEqualIdAndNewEntityState(id2, EntityState.Added));
    }

    [Fact]
    public Task ParentDoiWithSameTenantShouldThrow()
    {
        return AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(v =>
            {
                v.ContestId = ContestMockedData.IdGossau;
                v.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ChildDoiWithSameTenantShouldReturnOk()
    {
        var response = await AdminClient.CreateAsync(NewValidRequest(v =>
        {
            v.ContestId = ContestMockedData.IdStGallenEvoting;
            v.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau;
        }));

        var eventData = EventPublisherMock.GetSinglePublishedEvent<VoteCreated>();

        eventData.Vote.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.Vote.Id);
    }

    [Fact]
    public Task SiblingDoiWithSameTenantShouldThrow()
    {
        return AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(v =>
            {
                v.ContestId = ContestMockedData.IdStGallenEvoting;
                v.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdThurgau;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public Task ChildDoiWithDifferentTenantShouldThrow()
    {
        return AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(v =>
            {
                v.ContestId = ContestMockedData.IdStGallenEvoting;
                v.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdUzwil;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestBallotBundleSampleSizePercent100ShouldWork()
    {
        await AdminClient.CreateAsync(NewValidRequest(o => o.BallotBundleSampleSizePercent = 100));
    }

    [Fact]
    public async Task TestBallotBundleSampleSizePercent0ShouldWork()
    {
        await AdminClient.CreateAsync(NewValidRequest(o => o.BallotBundleSampleSizePercent = 0));
    }

    [Fact]
    public Task NoBallotsNotEnforcedResultEntryShouldThrow()
    {
        return AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o => o.EnforceResultEntryForCountingCircles = false)),
            StatusCode.InvalidArgument,
            "since the detailed result entry is not allowed for this vote, final result entry must be enforced");
    }

    [Fact]
    public Task NoBallotsDetailedResultEntryShouldThrow()
    {
        return AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o => o.ResultEntry = SharedProto.VoteResultEntry.Detailed)),
            StatusCode.InvalidArgument,
            "detailed result entry is only allowed if exactly one variants ballot exists");
    }

    [Fact]
    public async Task HighestReportDomainOfInfluenceLevelShouldWork()
    {
        var reportLevel = 1; // the St. Gallen domain of influence has 1 child level, so this should be ok

        var response = await AdminClient.CreateAsync(NewValidRequest(v => v.ReportDomainOfInfluenceLevel = reportLevel));
        var ev = EventPublisherMock.GetSinglePublishedEvent<VoteCreated>();

        ev.Vote.ReportDomainOfInfluenceLevel.Should().Be(reportLevel);
    }

    [Fact]
    public Task InvalidReportDomainOfInfluenceLevelShouldThrow()
    {
        return AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o => o.ReportDomainOfInfluenceLevel = 2)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task VoteInContestWithEndedTestingPhaseShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o =>
            {
                o.ContestId = ContestMockedData.IdPastUnlockedContest;
                o.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau;
            })),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new VoteService.VoteServiceClient(channel)
            .CreateAsync(NewValidRequest());

    private CreateVoteRequest NewValidRequest(
        Action<CreateVoteRequest>? customizer = null)
    {
        var request = new CreateVoteRequest
        {
            PoliticalBusinessNumber = "1338",
            OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Abstimmung") },
            ShortDescription = { LanguageUtil.MockAllLanguages("Neue Abst") },
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
            ContestId = ContestMockedData.IdStGallenEvoting,
            Active = true,
            ResultAlgorithm = SharedProto.VoteResultAlgorithm.PopularMajority,
            ResultEntry = SharedProto.VoteResultEntry.FinalResults,
            AutomaticBallotBundleNumberGeneration = true,
            BallotBundleSampleSizePercent = 50,
            EnforceResultEntryForCountingCircles = true,
            ReviewProcedure = SharedProto.VoteReviewProcedure.Physically,
            EnforceReviewProcedureForCountingCircles = true,
        };

        customizer?.Invoke(request);
        return request;
    }
}
