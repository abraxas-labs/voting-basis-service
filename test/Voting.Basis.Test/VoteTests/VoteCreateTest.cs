// (c) Copyright by Abraxas Informatik AG
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
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.VoteTests;

public class VoteCreateTest : PoliticalBusinessAuthorizationGrpcBaseTest<VoteService.VoteServiceClient>
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
        var response = await CantonAdminClient.CreateAsync(NewValidRequest());

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<VoteCreated, EventSignatureBusinessMetadata>();

        eventData.Vote.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.Vote.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestWithVariantQuestionsOnMultipleBallots()
    {
        var response = await CantonAdminClient.CreateAsync(NewValidRequest(x => x.Type = SharedProto.VoteType.VariantQuestionsOnMultipleBallots));

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<VoteCreated, EventSignatureBusinessMetadata>();

        eventData.Vote.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.Vote.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);

        await RunEvents<VoteCreated>();
        var id = Guid.Parse(eventData.Vote.Id);
        var simplePb = await RunOnDb(db => db.SimplePoliticalBusiness.FirstAsync(x => x.Id == id));
        simplePb.BusinessSubType.Should().Be(PoliticalBusinessSubType.VoteVariantBallot);

        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusiness.HasEqualIdAndNewEntityState(id, EntityState.Added)
                 && x.PoliticalBusiness!.Data!.BusinessSubType == PoliticalBusinessSubType.VoteVariantBallot);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await CantonAdminClient.CreateAsync(NewValidRequest());
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

        var vote1 = await CantonAdminClient.GetAsync(new GetVoteRequest
        {
            Id = id1.ToString(),
        });
        var vote2 = await CantonAdminClient.GetAsync(new GetVoteRequest
        {
            Id = id2.ToString(),
        });
        vote1.MatchSnapshot("1");
        vote2.MatchSnapshot("2");

        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusiness.HasEqualIdAndNewEntityState(id1, EntityState.Added));
        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusiness.HasEqualIdAndNewEntityState(id2, EntityState.Added)
                 && x.PoliticalBusiness!.Data!.BusinessSubType == PoliticalBusinessSubType.Unspecified);

        var simplePb = await RunOnDb(db => db.SimplePoliticalBusiness.FirstAsync(x => x.Id == id1));
        simplePb.BusinessSubType.Should().Be(PoliticalBusinessSubType.Unspecified);
    }

    [Fact]
    public Task ParentDoiWithSameTenantShouldThrow()
    {
        return AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRequest(v =>
            {
                v.ContestId = ContestMockedData.IdGossau;
                v.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen;
            })),
            StatusCode.InvalidArgument,
            "some ids are not children of the parent node");
    }

    [Fact]
    public async Task ChildDoiWithSameTenantShouldReturnOk()
    {
        var response = await CantonAdminClient.CreateAsync(NewValidRequest(v =>
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
            async () => await CantonAdminClient.CreateAsync(NewValidRequest(v =>
            {
                v.ContestId = ContestMockedData.IdStGallenEvoting;
                v.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdThurgau;
            })),
            StatusCode.InvalidArgument,
            "some ids are not children of the parent node");
    }

    [Fact]
    public async Task TestBallotBundleSampleSizePercent100ShouldWork()
    {
        await CantonAdminClient.CreateAsync(NewValidRequest(o => o.BallotBundleSampleSizePercent = 100));
    }

    [Fact]
    public async Task TestBallotBundleSampleSizePercent0ShouldWork()
    {
        await CantonAdminClient.CreateAsync(NewValidRequest(o => o.BallotBundleSampleSizePercent = 0));
    }

    [Fact]
    public Task NoBallotsNotEnforcedResultEntryShouldThrow()
    {
        return AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRequest(o => o.EnforceResultEntryForCountingCircles = false)),
            StatusCode.InvalidArgument,
            "since the detailed result entry is not allowed for this vote, final result entry must be enforced");
    }

    [Fact]
    public Task NoBallotsDetailedResultEntryShouldThrow()
    {
        return AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRequest(o => o.ResultEntry = SharedProto.VoteResultEntry.Detailed)),
            StatusCode.InvalidArgument,
            "detailed result entry is only allowed if exactly one variants ballot exists");
    }

    [Fact]
    public async Task HighestReportDomainOfInfluenceLevelShouldWork()
    {
        var reportLevel = 1; // the St. Gallen domain of influence has 1 child level, so this should be ok

        await CantonAdminClient.CreateAsync(NewValidRequest(v => v.ReportDomainOfInfluenceLevel = reportLevel));
        var ev = EventPublisherMock.GetSinglePublishedEvent<VoteCreated>();

        ev.Vote.ReportDomainOfInfluenceLevel.Should().Be(reportLevel);
    }

    [Fact]
    public Task InvalidReportDomainOfInfluenceLevelShouldThrow()
    {
        return AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRequest(o => o.ReportDomainOfInfluenceLevel = 2)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task VoteInContestWithEndedTestingPhaseShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRequest(o =>
            {
                o.ContestId = ContestMockedData.IdPastUnlockedContest;
                o.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau;
            })),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    [Fact]
    public async Task VirtualTopLevelDomainOfInfluenceShouldThrow()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            x => x.Id == DomainOfInfluenceMockedData.GuidStGallen,
            x => x.VirtualTopLevel = true);

        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRequest()),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public Task DuplicatePoliticalBusinessIdShouldThrow()
    {
        return AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRequest(v => v.PoliticalBusinessNumber = "155")),
            StatusCode.AlreadyExists);
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
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
            Type = SharedProto.VoteType.QuestionsOnSingleBallot,
        };

        customizer?.Invoke(request);
        return request;
    }
}
