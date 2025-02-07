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
using Voting.Basis.Core.Auth;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionListUnionUpdateTest : PoliticalBusinessAuthorizationGrpcBaseTest<ProportionalElectionService.ProportionalElectionServiceClient>
{
    public ProportionalElectionListUnionUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ProportionalElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task ListUnionShouldReturnOk()
    {
        await CantonAdminClient.UpdateListUnionAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListUnionUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task SubListUnionShouldReturnOk()
    {
        await CantonAdminClient.UpdateListUnionAsync(NewValidRequest(lu =>
        {
            lu.Id = ProportionalElectionMockedData.SubListUnion11IdGossauProportionalElectionInContestStGallen;
            lu.ProportionalElectionRootListUnionId = ProportionalElectionMockedData.ListUnion1IdGossauProportionalElectionInContestStGallen;
        }));

        var eventData = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListUnionUpdated>();
        eventData.MatchSnapshot("event");
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await CantonAdminClient.UpdateListUnionAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionListUnionUpdated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(
            new ProportionalElectionListUnionUpdated
            {
                ProportionalElectionListUnion = new ProportionalElectionListUnionEventData
                {
                    Id = ProportionalElectionMockedData.ListUnionIdStGallenProportionalElectionInContestBund,
                    Position = 1,
                    ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestBund,
                    Description = { LanguageUtil.MockAllLanguages("Updated list union") },
                },
            },
            new ProportionalElectionListUnionUpdated
            {
                ProportionalElectionListUnion = new ProportionalElectionListUnionEventData
                {
                    Id = ProportionalElectionMockedData.SubListUnion11IdGossauProportionalElectionInContestStGallen,
                    Position = 1,
                    ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
                    Description = { LanguageUtil.MockAllLanguages("Updated sub list union") },
                },
            });

        var listUnion1 = await CantonAdminClient.GetListUnionAsync(new GetProportionalElectionListUnionRequest
        {
            Id = ProportionalElectionMockedData.ListUnionIdStGallenProportionalElectionInContestBund,
        });
        var listUnion2 = await CantonAdminClient.GetListUnionAsync(new GetProportionalElectionListUnionRequest
        {
            Id = ProportionalElectionMockedData.SubListUnion11IdGossauProportionalElectionInContestStGallen,
        });

        listUnion1.MatchSnapshot("1");
        listUnion2.MatchSnapshot("2");
    }

    [Fact]
    public async Task ListUnionInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await CantonAdminClient.UpdateListUnionAsync(new UpdateProportionalElectionListUnionRequest
            {
                Id = ProportionalElectionMockedData.ListUnionIdGossauProportionalElectionInContestBund,
                ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund,
                Description = { LanguageUtil.MockAllLanguages("Updated list union 1") },
                Position = 1,
            }),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    [Fact]
    public async Task ListUnionInNonHagenbachBischoffElectionShouldThrow()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            doi => doi.Id == DomainOfInfluenceMockedData.GuidGossau,
            doi => doi.CantonDefaults.ProportionalElectionMandateAlgorithms = new()
            {
                ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            });

        await ElectionAdminClient.UpdateAsync(new()
        {
            Id = ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund,
            PoliticalBusinessNumber = "1",
            NumberOfMandates = 2,
            MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            BallotNumberGeneration = SharedProto.BallotNumberGeneration.RestartForEachBundle,
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
            ContestId = ContestMockedData.IdBundContest,
            ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Electronically,
        });

        await AssertStatus(
            async () => await ElectionAdminClient.UpdateListUnionAsync(new()
            {
                Id = ProportionalElectionMockedData.ListUnionIdGossauProportionalElectionInContestBund,
                ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund,
                Position = 1,
            }),
            StatusCode.InvalidArgument,
            "The election does not distribute mandates per Hagenbach-Bischoff algorithm");
    }

    [Fact]
    public async Task ChangeProportionalElectionIdShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateListUnionAsync(NewValidRequest(l =>
                l.ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen)),
            StatusCode.InvalidArgument,
            "ListUnion 16892ba3-9b8c-42c7-914e-4b4692d170f4 does not exist");
    }

    [Fact]
    public async Task ChangeRootIdShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateListUnionAsync(NewValidRequest(l =>
            {
                l.Id = ProportionalElectionMockedData.SubListUnion21IdGossauProportionalElectionInContestStGallen;
                l.ProportionalElectionRootListUnionId = ProportionalElectionMockedData.ListUnion1IdGossauProportionalElectionInContestStGallen;
                l.Position = 1;
            })),
            StatusCode.InvalidArgument,
            "RootId");
    }

    [Fact]
    public async Task ChangePositionShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateListUnionAsync(NewValidRequest(o => o.Position = 2)),
            StatusCode.InvalidArgument);
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new ProportionalElectionService.ProportionalElectionServiceClient(channel)
            .UpdateListUnionAsync(NewValidRequest());

    private UpdateProportionalElectionListUnionRequest NewValidRequest(
        Action<UpdateProportionalElectionListUnionRequest>? customizer = null)
    {
        var request = new UpdateProportionalElectionListUnionRequest
        {
            Id = ProportionalElectionMockedData.ListUnion1IdGossauProportionalElectionInContestStGallen,
            Position = 1,
            ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
            Description = { LanguageUtil.MockAllLanguages("Updated list union 1") },
        };

        customizer?.Invoke(request);
        return request;
    }
}
