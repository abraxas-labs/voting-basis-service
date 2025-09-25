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
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.MajorityElectionTests;

public class MajorityElectionActiveStateUpdateTest : PoliticalBusinessAuthorizationGrpcBaseTest<MajorityElectionService.MajorityElectionServiceClient>
{
    public MajorityElectionActiveStateUpdateTest(TestApplicationFactory factory)
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
        await ElectionAdminClient.UpdateActiveStateAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionActiveStateUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await ElectionAdminClient.UpdateActiveStateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionActiveStateUpdated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = Guid.Parse(MajorityElectionMockedData.IdGossauMajorityElectionInContestStGallen);

        await TestEventPublisher.Publish(
            new MajorityElectionActiveStateUpdated
            {
                MajorityElectionId = id.ToString(),
                Active = true,
            });
        var response = await ElectionAdminClient.GetAsync(new GetMajorityElectionRequest { Id = MajorityElectionMockedData.IdGossauMajorityElectionInContestStGallen });
        response.MatchSnapshot();

        await AssertHasPublishedEventProcessedMessage(MajorityElectionActiveStateUpdated.Descriptor, id);
    }

    [Fact]
    public async Task MajorityElectionInContestWithEndedTestingPhasetShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateActiveStateAsync(NewValidRequest(o =>
            {
                o.Id = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund;
            })),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    [Fact]
    public async Task ModificationWithEVotingApprovedShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateActiveStateAsync(NewValidRequest(x =>
            {
                x.Id = MajorityElectionMockedData.IdGossauMajorityElectionEVotingApprovedInContestStGallen;
            })),
            StatusCode.FailedPrecondition,
            nameof(PoliticalBusinessEVotingApprovedException));
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
            async () => await ElectionAdminClient.UpdateActiveStateAsync(new UpdateMajorityElectionActiveStateRequest
            {
                Id = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund,
                Active = true,
            }),
            StatusCode.FailedPrecondition,
            nameof(MajorityElectionBallotGroupVoteCountException));
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .UpdateActiveStateAsync(NewValidRequest());

    private UpdateMajorityElectionActiveStateRequest NewValidRequest(
        Action<UpdateMajorityElectionActiveStateRequest>? customizer = null)
    {
        var request = new UpdateMajorityElectionActiveStateRequest
        {
            Id = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
            Active = true,
        };

        customizer?.Invoke(request);
        return request;
    }
}
