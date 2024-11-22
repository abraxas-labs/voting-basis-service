// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionDeleteTest : PoliticalBusinessAuthorizationGrpcBaseTest<ProportionalElectionService.ProportionalElectionServiceClient>
{
    private const string IdNotFound = "bfe2cfaf-c787-48b9-a108-c975b0addddd";
    private string? _authTestElectionId;

    public ProportionalElectionDeleteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ProportionalElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestNotFound()
    {
        await AssertStatus(
            async () => await AdminClient.DeleteAsync(new DeleteProportionalElectionRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task Test()
    {
        await AdminClient.DeleteAsync(new DeleteProportionalElectionRequest
        {
            Id = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
        });
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionDeleted, EventSignatureBusinessMetadata>();

        eventData.ProportionalElectionId.Should().Be(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen);
        eventData.MatchSnapshot();
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.DeleteAsync(new DeleteProportionalElectionRequest
            {
                Id = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
            });
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionDeleted>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen;
        await TestEventPublisher.Publish(new ProportionalElectionDeleted { ProportionalElectionId = id });

        var idGuid = Guid.Parse(id);
        (await RunOnDb(db => db.ProportionalElections.CountAsync(c => c.Id == idGuid)))
            .Should().Be(0);

        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusiness.HasEqualIdAndNewEntityState(idGuid, EntityState.Deleted));
    }

    [Fact]
    public async Task TestAggregateUnionLists()
    {
        var id = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen;
        await TestEventPublisher.Publish(new ProportionalElectionDeleted { ProportionalElectionId = id });

        (await RunOnDb(db =>
            db.ProportionalElectionUnionLists
                .Where(l => ProportionalElectionUnionMockedData.StGallen1.Id == l.ProportionalElectionUnionId)
                .Select(l => new { l.OrderNumber, l.ShortDescription })
                .OrderBy(x => x.OrderNumber)
                .ToListAsync())).MatchSnapshot();
    }

    [Fact]
    public async Task ProportionalElectionInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteAsync(new DeleteProportionalElectionRequest
            {
                Id = ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund,
            }),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        if (_authTestElectionId == null)
        {
            var response = await ElectionAdminClient.CreateAsync(new CreateProportionalElectionRequest
            {
                PoliticalBusinessNumber = "4687",
                OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Proporzwahl") },
                ShortDescription = { LanguageUtil.MockAllLanguages("Proporzwahl") },
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                ContestId = ContestMockedData.IdStGallenEvoting,
                Active = true,
                AutomaticBallotBundleNumberGeneration = true,
                AutomaticEmptyVoteCounting = true,
                BallotBundleSize = 13,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
                CandidateCheckDigit = true,
                EnforceEmptyVoteCountingForCountingCircles = true,
                MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.HagenbachBischoff,
                NumberOfMandates = 5,
                ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Electronically,
                EnforceReviewProcedureForCountingCircles = true,
                EnforceCandidateCheckDigitForCountingCircles = true,
            });

            _authTestElectionId = response.Id;
        }

        await new ProportionalElectionService.ProportionalElectionServiceClient(channel)
            .DeleteAsync(new DeleteProportionalElectionRequest { Id = _authTestElectionId });
        _authTestElectionId = null;
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }
}
