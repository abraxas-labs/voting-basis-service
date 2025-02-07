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

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionListUnionCreateTest : PoliticalBusinessAuthorizationGrpcBaseTest<ProportionalElectionService.ProportionalElectionServiceClient>
{
    public ProportionalElectionListUnionCreateTest(TestApplicationFactory factory)
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
        var response = await CantonAdminClient.CreateListUnionAsync(NewValidRequest());

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListUnionCreated, EventSignatureBusinessMetadata>();

        eventData.ProportionalElectionListUnion.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.ProportionalElectionListUnion.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task SubListUnionShouldReturnOk()
    {
        var response = await CantonAdminClient.CreateListUnionAsync(NewValidRequest(o =>
        {
            o.ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen;
            o.Position = 3;
            o.ProportionalElectionRootListUnionId = ProportionalElectionMockedData.ListUnion1IdGossauProportionalElectionInContestStGallen;
        }));

        var eventData = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListUnionCreated>();

        eventData.ProportionalElectionListUnion.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.ProportionalElectionListUnion.Id);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await CantonAdminClient.CreateListUnionAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionListUnionCreated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(
            new ProportionalElectionListUnionCreated
            {
                ProportionalElectionListUnion = new ProportionalElectionListUnionEventData
                {
                    Id = "430f11f8-82bf-4f39-a2b9-d76e8c9dab08",
                    Position = 1,
                    ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallenWithoutChilds,
                    Description = { LanguageUtil.MockAllLanguages("Created list union") },
                },
            },
            new ProportionalElectionListUnionCreated
            {
                ProportionalElectionListUnion = new ProportionalElectionListUnionEventData
                {
                    Id = "c995e944-5b49-40f8-a75b-814a10ebc0f0",
                    Position = 2,
                    ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallenWithoutChilds,
                    Description = { LanguageUtil.MockAllLanguages("Created list union 2") },
                },
            });

        var listUnion1 = await CantonAdminClient.GetListUnionAsync(new GetProportionalElectionListUnionRequest
        {
            Id = "430f11f8-82bf-4f39-a2b9-d76e8c9dab08",
        });
        var listUnion2 = await CantonAdminClient.GetListUnionAsync(new GetProportionalElectionListUnionRequest
        {
            Id = "c995e944-5b49-40f8-a75b-814a10ebc0f0",
        });
        listUnion1.MatchSnapshot("1");
        listUnion2.MatchSnapshot("2");
    }

    [Fact]
    public async Task SubListUnionInSubListUnionShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateListUnionAsync(NewValidRequest(lu =>
            {
                lu.ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen;
                lu.ProportionalElectionRootListUnionId = ProportionalElectionMockedData.SubListUnion11IdGossauProportionalElectionInContestStGallen;
            })),
            StatusCode.InvalidArgument,
            "SubListUnion");
    }

    [Fact]
    public async Task NonContinuousPositionShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateListUnionAsync(NewValidRequest(o =>
            {
                // this proportional election already has 2 list unions, so the list position can't be 2
                o.ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen;
                o.Position = 2;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ListUnionInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await CantonAdminClient.CreateListUnionAsync(NewValidRequest(o =>
            {
                o.ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund;
                o.Position = 2;
            })),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    [Fact]
    public async Task ListUnionInNonHagenbachBischoffElectionShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminUzwilClient.CreateListUnionAsync(NewValidRequest(o => o.ProportionalElectionId = ProportionalElectionMockedData.IdUzwilProportionalElectionInContestUzwilWithoutChilds)),
            StatusCode.InvalidArgument,
            "The election does not distribute mandates per Hagenbach-Bischoff algorithm");
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        var response = await new ProportionalElectionService.ProportionalElectionServiceClient(channel)
            .CreateListUnionAsync(NewValidRequest());
        await RunEvents<ProportionalElectionListUnionCreated>();

        await ElectionAdminClient.DeleteListUnionAsync(new DeleteProportionalElectionListUnionRequest
        {
            Id = response.Id,
        });
    }

    private CreateProportionalElectionListUnionRequest NewValidRequest(
        Action<CreateProportionalElectionListUnionRequest>? customizer = null)
    {
        var request = new CreateProportionalElectionListUnionRequest
        {
            Position = 3,
            ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallenWithoutChilds,
            Description = { LanguageUtil.MockAllLanguages("Created list union") },
        };

        customizer?.Invoke(request);
        return request;
    }
}
