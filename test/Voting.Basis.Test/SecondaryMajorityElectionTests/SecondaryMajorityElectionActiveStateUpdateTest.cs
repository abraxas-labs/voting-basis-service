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
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionActiveStateUpdateTest : PoliticalBusinessAuthorizationGrpcBaseTest<MajorityElectionService.MajorityElectionServiceClient>
{
    public SecondaryMajorityElectionActiveStateUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await MajorityElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task Test()
    {
        await CantonAdminClient.UpdateSecondaryMajorityElectionActiveStateAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionActiveStateUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdBundContest);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdBundContest, async () =>
        {
            await CantonAdminClient.UpdateSecondaryMajorityElectionActiveStateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<SecondaryMajorityElectionActiveStateUpdated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(
            new SecondaryMajorityElectionActiveStateUpdated
            {
                SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                Active = true,
            });
        var response = await CantonAdminClient.GetSecondaryMajorityElectionAsync(new GetSecondaryMajorityElectionRequest { Id = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task SetActiveWithInvalidBallotGroupVoteCountsShouldThrow()
    {
        await CantonAdminClient.CreateBallotGroupAsync(new CreateMajorityElectionBallotGroupRequest
        {
            Description = "test new",
            Position = 3,
            ShortDescription = "short - long",
            MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund,
            Entries =
                {
                    new ProtoModels.MajorityElectionBallotGroupEntry
                    {
                        ElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund,
                        Id = MajorityElectionMockedData.BallotGroupEntryId21GossauMajorityElectionInContestBund,
                    },
                    new ProtoModels.MajorityElectionBallotGroupEntry
                    {
                        ElectionId = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionInContestBund,
                        Id = MajorityElectionMockedData.BallotGroupEntryId22GossauMajorityElectionInContestBund,
                    },
                },
        });

        await AssertStatus(
            async () => await ElectionAdminClient.UpdateSecondaryMajorityElectionActiveStateAsync(new UpdateSecondaryMajorityElectionActiveStateRequest
            {
                Id = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionInContestBund,
                Active = true,
            }),
            StatusCode.FailedPrecondition,
            nameof(MajorityElectionBallotGroupVoteCountException));
    }

    [Fact]
    public async Task ModificationWithEVotingApprovedShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateSecondaryMajorityElectionActiveStateAsync(NewValidRequest(x =>
            {
                x.Id = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionEVotingApprovedInContestStGallen;
            })),
            StatusCode.FailedPrecondition,
            nameof(PoliticalBusinessEVotingApprovedException));
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .UpdateSecondaryMajorityElectionActiveStateAsync(NewValidRequest());

    private UpdateSecondaryMajorityElectionActiveStateRequest NewValidRequest(
        Action<UpdateSecondaryMajorityElectionActiveStateRequest>? customizer = null)
    {
        var request = new UpdateSecondaryMajorityElectionActiveStateRequest
        {
            Id = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
            Active = true,
        };

        customizer?.Invoke(request);
        return request;
    }
}
