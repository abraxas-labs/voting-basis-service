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
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionListUnionDeleteTest : PoliticalBusinessAuthorizationGrpcBaseTest<ProportionalElectionService.ProportionalElectionServiceClient>
{
    private const string IdNotFound = "bfe2cfaf-c787-48b9-a108-c975b0addddd";
    private string? _authTestListUnionId;

    public ProportionalElectionListUnionDeleteTest(TestApplicationFactory factory)
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
            async () => await CantonAdminClient.DeleteListUnionAsync(new DeleteProportionalElectionListUnionRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task Test()
    {
        await CantonAdminClient.DeleteListUnionAsync(new DeleteProportionalElectionListUnionRequest
        {
            Id = ProportionalElectionMockedData.ListUnion2IdGossauProportionalElectionInContestStGallen,
        });
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListUnionDeleted, EventSignatureBusinessMetadata>();

        eventData.ProportionalElectionListUnionId.Should().Be(ProportionalElectionMockedData.ListUnion2IdGossauProportionalElectionInContestStGallen);
        eventData.MatchSnapshot();
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await CantonAdminClient.DeleteListUnionAsync(new DeleteProportionalElectionListUnionRequest
            {
                Id = ProportionalElectionMockedData.ListUnion2IdGossauProportionalElectionInContestStGallen,
            });
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionListUnionDeleted>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = ProportionalElectionMockedData.ListUnion1IdGossauProportionalElectionInContestStGallen;

        await TestEventPublisher.Publish(new ProportionalElectionListUnionDeleted { ProportionalElectionListUnionId = id });

        var idGuid = Guid.Parse(id);
        (await RunOnDb(db => db.ProportionalElectionListUnions.CountAsync(c => c.Id == idGuid || c.ProportionalElectionRootListUnionId == idGuid)))
            .Should().Be(0);
    }

    [Fact]
    public async Task ListUnionInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteListUnionAsync(new DeleteProportionalElectionListUnionRequest
            {
                Id = ProportionalElectionMockedData.ListUnionIdGossauProportionalElectionInContestBund,
            }),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    [Fact]
    public async Task ModificationWithEVotingApprovedShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.DeleteListUnionAsync(new DeleteProportionalElectionListUnionRequest
            {
                Id = ProportionalElectionMockedData.ListUnionIdGossauProportionalElectionEVotingApprovedInContestStGallen,
            }),
            StatusCode.FailedPrecondition,
            nameof(PoliticalBusinessEVotingApprovedException));
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        if (_authTestListUnionId == null)
        {
            var response = await ElectionAdminClient.CreateListUnionAsync(new CreateProportionalElectionListUnionRequest
            {
                Position = 3,
                ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallenWithoutChilds,
                Description = { LanguageUtil.MockAllLanguages("Created list union") },
            });
            await RunEvents<ProportionalElectionListUnionCreated>();

            _authTestListUnionId = response.Id;
        }

        await new ProportionalElectionService.ProportionalElectionServiceClient(channel)
            .DeleteListUnionAsync(new DeleteProportionalElectionListUnionRequest { Id = _authTestListUnionId });
        _authTestListUnionId = null;
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }
}
