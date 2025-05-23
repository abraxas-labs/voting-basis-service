﻿// (c) Copyright by Abraxas Informatik AG
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

public class MajorityElectionBallotGroupCreateTest : PoliticalBusinessAuthorizationGrpcBaseTest<MajorityElectionService.MajorityElectionServiceClient>
{
    private int _authTestPosition = 1;

    public MajorityElectionBallotGroupCreateTest(TestApplicationFactory factory)
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
        var response = await ElectionAdminClient.CreateBallotGroupAsync(NewValidRequest());

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionBallotGroupCreated, EventSignatureBusinessMetadata>();

        // ignore server generated ids for the snapshot comparison
        foreach (var entry in eventData.BallotGroup.Entries)
        {
            entry.Id = string.Empty;
        }

        foreach (var entry in response.Entries)
        {
            entry.Id = string.Empty;
        }

        eventData.BallotGroup.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.BallotGroup.Id);
        response.MatchSnapshot("response", d => d.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestAggregate()
    {
        var ev = new MajorityElectionBallotGroupCreated
        {
            BallotGroup = new MajorityElectionBallotGroupEventData
            {
                Id = "5b2f0f6b-dd7c-4a8e-8887-7f13917e700f",
                Position = 1,
                Description = "test",
                MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                ShortDescription = "short desc",
            },
        };
        ev.BallotGroup.Entries.Add(new MajorityElectionBallotGroupEntryEventData
        {
            ElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
            Id = "323b8aa8-d73b-4b9d-8926-d332bd8f62d2",
        });
        await TestEventPublisher.Publish(ev);

        var ballotGroups = await ElectionAdminClient.ListBallotGroupsAsync(new ListMajorityElectionBallotGroupsRequest
        {
            MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
        });

        ballotGroups.MatchSnapshot();
    }

    [Fact]
    public async Task TestProcessorDeprecatedEventWithBlankRowCount()
    {
        var ev = new MajorityElectionBallotGroupCreated
        {
            BallotGroup = new MajorityElectionBallotGroupEventData
            {
                Id = "5b2f0f6b-dd7c-4a8e-8887-7f13917e700f",
                Position = 1,
                Description = "test",
                MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                ShortDescription = "short desc",
                Entries =
                    {
                        new MajorityElectionBallotGroupEntryEventData
                        {
                            BlankRowCount = 5,
                            ElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                            Id = "323b8aa8-d73b-4b9d-8926-d332bd8f62d2",
                        },
                    },
            },
        };
        await TestEventPublisher.Publish(ev);

        var ballotGroups = await ElectionAdminClient.ListBallotGroupsAsync(new ListMajorityElectionBallotGroupsRequest
        {
            MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
        });

        var entry = ballotGroups.BallotGroups.Single().Entries.Single();
        entry.BlankRowCount.Should().Be(5);
    }

    [Fact]
    public async Task NonContinuousPositionShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.CreateBallotGroupAsync(NewValidRequest(o => o.Position = 18)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task MissingEntriesShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.CreateBallotGroupAsync(NewValidRequest(o => o.Entries.Clear())),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task DuplicateEntriesShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.CreateBallotGroupAsync(NewValidRequest(o => o.Entries.Add(o.Entries[0]))),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task NonRelatedEntryShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.CreateBallotGroupAsync(NewValidRequest(o =>
            o.Entries.Add(new ProtoModels.MajorityElectionBallotGroupEntry
            {
                BlankRowCount = 0,
                ElectionId = MajorityElectionMockedData.IdGenfMajorityElectionInContestBundWithoutChilds,
            }))),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task MajorityElectionInPastContestShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await ElectionAdminClient.CreateBallotGroupAsync(new CreateMajorityElectionBallotGroupRequest
        {
            Description = "past",
            Position = 3,
            ShortDescription = "past",
            MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund,
            Entries =
                {
                    new ProtoModels.MajorityElectionBallotGroupEntry
                    {
                        BlankRowCount = 0,
                        ElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund,
                    },
                    new ProtoModels.MajorityElectionBallotGroupEntry
                    {
                        BlankRowCount = 0,
                        ElectionId = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionInContestBund,
                    },
                },
        });
    }

    [Fact]
    public async Task MajorityElectionInLockedContestShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastLocked);
        await AssertStatus(
            async () => await ElectionAdminClient.CreateBallotGroupAsync(new CreateMajorityElectionBallotGroupRequest
            {
                Description = "past",
                Position = 2,
                ShortDescription = "past",
                MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund,
                Entries =
                {
                        new ProtoModels.MajorityElectionBallotGroupEntry
                        {
                            BlankRowCount = 0,
                            ElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund,
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
    {
        await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .CreateBallotGroupAsync(NewValidRequest(req => req.Position = _authTestPosition++));
    }

    private CreateMajorityElectionBallotGroupRequest NewValidRequest(
        Action<CreateMajorityElectionBallotGroupRequest>? customizer = null)
    {
        var request = new CreateMajorityElectionBallotGroupRequest
        {
            Description = "test",
            Position = 1,
            ShortDescription = "short",
            MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
            Entries =
                {
                    new ProtoModels.MajorityElectionBallotGroupEntry
                    {
                        BlankRowCount = 0,
                        ElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                    },
                },
        };

        customizer?.Invoke(request);
        return request;
    }
}
