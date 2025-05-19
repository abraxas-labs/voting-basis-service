// (c) Copyright by Abraxas Informatik AG
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
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Basis.Core.Auth;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.MajorityElectionBallotGroupTests;

public class MajorityElectionBallotGroupUpdateTest : PoliticalBusinessAuthorizationGrpcBaseTest<MajorityElectionService.MajorityElectionServiceClient>
{
    public MajorityElectionBallotGroupUpdateTest(TestApplicationFactory factory)
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
        var response = await CantonAdminClient.UpdateBallotGroupAsync(NewValidRequest());

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionBallotGroupUpdated, EventSignatureBusinessMetadata>();

        eventData.MatchSnapshot("event");
        response.MatchSnapshot("response");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdBundContest);
    }

    [Fact]
    public async Task TestAggregate()
    {
        var ev = new MajorityElectionBallotGroupUpdated
        {
            BallotGroup = new MajorityElectionBallotGroupEventData
            {
                Description = "test new",
                Position = 1,
                ShortDescription = "short - long",
                MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
                Id = MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund,
                BlankRowCountUnused = true,
            },
        };
        ev.BallotGroup.Entries.Add(new MajorityElectionBallotGroupEntryEventData
        {
            ElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
            Id = MajorityElectionMockedData.BallotGroupEntryId1StGallenMajorityElectionInContestBund,
        });
        ev.BallotGroup.Entries.Add(new MajorityElectionBallotGroupEntryEventData
        {
            ElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
            Id = MajorityElectionMockedData.BallotGroupEntryId2StGallenMajorityElectionInContestBund,
        });

        await TestEventPublisher.Publish(ev);

        var ballotGroups = await CantonAdminClient.ListBallotGroupsAsync(new ListMajorityElectionBallotGroupsRequest
        {
            MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
        });

        var ballotGroupEntry2 = ballotGroups
            .BallotGroups
            .Single(x => x.Id == MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund)
            .Entries
            .Single(x => x.Id == MajorityElectionMockedData.BallotGroupEntryId2StGallenMajorityElectionInContestBund);

        // The candidate vote counts should remain unchanged.
        ballotGroupEntry2.BlankRowCount.Should().Be(2);

        ballotGroups.MatchSnapshot();
    }

    [Fact]
    public async Task TestProcessorDeprecatedEventWithBlankRowCount()
    {
        var ev = new MajorityElectionBallotGroupUpdated
        {
            BallotGroup = new MajorityElectionBallotGroupEventData
            {
                Description = "test new",
                Position = 1,
                ShortDescription = "short - long",
                MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
                Id = MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund,
                BlankRowCountUnused = false,
            },
        };
        ev.BallotGroup.Entries.Add(new MajorityElectionBallotGroupEntryEventData
        {
            BlankRowCount = 1,
            ElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
            Id = MajorityElectionMockedData.BallotGroupEntryId1StGallenMajorityElectionInContestBund,
        });
        ev.BallotGroup.Entries.Add(new MajorityElectionBallotGroupEntryEventData
        {
            BlankRowCount = 1,
            ElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
            Id = MajorityElectionMockedData.BallotGroupEntryId2StGallenMajorityElectionInContestBund,
        });

        await TestEventPublisher.Publish(ev);

        var ballotGroups = await CantonAdminClient.ListBallotGroupsAsync(new ListMajorityElectionBallotGroupsRequest
        {
            MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
        });

        ballotGroups.MatchSnapshot();
    }

    [Fact]
    public async Task NonContinuousPositionShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateBallotGroupAsync(NewValidRequest(o => o.Position = 18)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task MissingEntriesShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateBallotGroupAsync(NewValidRequest(o => o.Entries.Clear())),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task DuplicateEntriesShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateBallotGroupAsync(NewValidRequest(o => o.Entries.Add(o.Entries[0]))),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task NonRelatedEntryShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateBallotGroupAsync(NewValidRequest(o =>
            o.Entries.Add(new ProtoModels.MajorityElectionBallotGroupEntry
            {
                BlankRowCount = 0,
                ElectionId = MajorityElectionMockedData.IdGenfMajorityElectionInContestBundWithoutChilds,
            }))),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task BallotGroupInLockedContestShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.Archived);
        await AssertStatus(
            async () => await CantonAdminClient.UpdateBallotGroupAsync(new UpdateMajorityElectionBallotGroupRequest
            {
                Description = "test new",
                Position = 1,
                ShortDescription = "short - long",
                MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund,
                Id = MajorityElectionMockedData.BallotGroupEntryId12GossauMajorityElectionInContestBund,
                Entries =
                {
                        new ProtoModels.MajorityElectionBallotGroupEntry
                        {
                            BlankRowCount = 0,
                            ElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund,
                            Id = MajorityElectionMockedData.BallotGroupEntryId11GossauMajorityElectionInContestBund,
                        },
                        new ProtoModels.MajorityElectionBallotGroupEntry
                        {
                            BlankRowCount = 0,
                            ElectionId = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionInContestBund,
                            Id = MajorityElectionMockedData.BallotGroupEntryId21GossauMajorityElectionInContestBund,
                        },
                },
            }),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .UpdateBallotGroupAsync(NewValidRequest());

    private UpdateMajorityElectionBallotGroupRequest NewValidRequest(
        Action<UpdateMajorityElectionBallotGroupRequest>? customizer = null)
    {
        var request = new UpdateMajorityElectionBallotGroupRequest
        {
            Description = "test new",
            Position = 1,
            ShortDescription = "short - long",
            MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
            Id = MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund,
            Entries =
                {
                    new ProtoModels.MajorityElectionBallotGroupEntry
                    {
                        ElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
                        Id = MajorityElectionMockedData.BallotGroupEntryId1StGallenMajorityElectionInContestBund,
                    },
                    new ProtoModels.MajorityElectionBallotGroupEntry
                    {
                        ElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                        Id = MajorityElectionMockedData.BallotGroupEntryId2StGallenMajorityElectionInContestBund,
                    },
                },
        };

        customizer?.Invoke(request);
        return request;
    }
}
