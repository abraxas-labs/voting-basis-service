// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
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

public class VoteDeleteTest : PoliticalBusinessAuthorizationGrpcBaseTest<VoteService.VoteServiceClient>
{
    private const string IdNotFound = "bfe2cfaf-c787-48b9-a108-c975b0addddd";
    private string? _authTestVoteId;

    public VoteDeleteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await VoteMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestNotFound()
    {
        await AssertStatus(
            async () => await AdminClient.DeleteAsync(new DeleteVoteRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task Test()
    {
        await AdminClient.DeleteAsync(new DeleteVoteRequest
        {
            Id = VoteMockedData.IdStGallenVoteInContestStGallen,
        });
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<VoteDeleted, EventSignatureBusinessMetadata>();

        eventData.VoteId.Should().Be(VoteMockedData.IdStGallenVoteInContestStGallen);
        eventData.MatchSnapshot();
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.DeleteAsync(new DeleteVoteRequest
            {
                Id = VoteMockedData.IdStGallenVoteInContestStGallen,
            });
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteDeleted>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = VoteMockedData.IdStGallenVoteInContestStGallen;
        await TestEventPublisher.Publish(new VoteDeleted { VoteId = id });

        var idGuid = Guid.Parse(id);
        (await RunOnDb(db => db.Votes.CountAsync(c => c.Id == idGuid)))
            .Should().Be(0);

        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusiness.HasEqualIdAndNewEntityState(idGuid, EntityState.Deleted));
    }

    [Fact]
    public async Task VoteInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteAsync(new DeleteVoteRequest
            {
                Id = VoteMockedData.IdGossauVoteInContestBund,
            }),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        if (_authTestVoteId == null)
        {
            var response = await ElectionAdminClient.CreateAsync(new CreateVoteRequest
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
            });
            _authTestVoteId = response.Id;
        }

        await new VoteService.VoteServiceClient(channel)
            .DeleteAsync(new DeleteVoteRequest { Id = _authTestVoteId });
        _authTestVoteId = null;
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }
}
