// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.MajorityElectionBallotGroupTests;

public class MajorityElectionBallotGroupCandidatesUpdateTest : BaseGrpcTest<MajorityElectionService.MajorityElectionServiceClient>
{
    public MajorityElectionBallotGroupCandidatesUpdateTest(TestApplicationFactory factory)
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
        var request = NewValidRequest();
        await AdminClient.UpdateBallotGroupCandidatesAsync(request);

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionBallotGroupCandidatesUpdated, EventSignatureBusinessMetadata>();

        eventData.BallotGroupCandidates.BallotGroupId.Should().Be(request.BallotGroupId);
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdBundContest);
    }

    [Fact]
    public async Task TestWithIndividualCandidate()
    {
        await AdminClient.UpdateBallotGroupCandidatesAsync(NewValidRequest(x => x.EntryCandidates[0].IndividualCandidatesVoteCount = 2));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionBallotGroupCandidatesUpdated>();
        eventData.MatchSnapshot("event");
    }

    [Fact]
    public async Task TestAggregate()
    {
        var ev = new MajorityElectionBallotGroupCandidatesUpdated
        {
            BallotGroupCandidates = new MajorityElectionBallotGroupCandidatesEventData
            {
                BallotGroupId = MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund,
                EntryCandidates =
                    {
                        new MajorityElectionBallotGroupEntryCandidatesEventData
                        {
                            BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId1StGallenMajorityElectionInContestBund,
                            CandidateIds = { MajorityElectionMockedData.CandidateId2StGallenMajorityElectionInContestBund },
                        },
                        new MajorityElectionBallotGroupEntryCandidatesEventData
                        {
                            BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId2StGallenMajorityElectionInContestBund,
                            CandidateIds = { MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund },
                            IndividualCandidatesVoteCount = 2,
                        },
                    },
            },
        };

        await TestEventPublisher.Publish(ev);

        var ballotGroupCandidates = await AdminClient.ListBallotGroupCandidatesAsync(new ListMajorityElectionBallotGroupCandidatesRequest
        {
            BallotGroupId = MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund,
        });

        ballotGroupCandidates.MatchSnapshot();

        var ballotGroups = await AdminClient.ListBallotGroupsAsync(new ListMajorityElectionBallotGroupsRequest
        {
            MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
        });

        var ballotGroupEntry = ballotGroups.BallotGroups
            .Single(x => x.Id == MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund)
            .Entries
            .Single(x => x.Id == MajorityElectionMockedData.BallotGroupEntryId2StGallenMajorityElectionInContestBund);
        ballotGroupEntry.CountOfCandidates.Should().Be(1);
        ballotGroupEntry.IndividualCandidatesVoteCount.Should().Be(2);
        ballotGroupEntry.CandidateCountOk.Should().BeTrue();
    }

    [Fact]
    public async Task MajorityElectionFromOtherDoiShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateBallotGroupCandidatesAsync(NewValidRequest(pe =>
             {
                 pe.BallotGroupId = MajorityElectionMockedData.BallotGroupIdKircheMajorityElectionInContestKirche;
             })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task MissingEntriesShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateBallotGroupCandidatesAsync(NewValidRequest(o => o.EntryCandidates.Clear())),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task DuplicateEntriesShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateBallotGroupCandidatesAsync(NewValidRequest(o =>
            o.EntryCandidates.Add(o.EntryCandidates[0]))),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task BallotGroupWithCandidateCountNotOkInPastContestShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AdminClient.UpdateBallotGroupCandidatesAsync(new UpdateMajorityElectionBallotGroupCandidatesRequest
        {
            BallotGroupId = MajorityElectionMockedData.BallotGroupId2GossauMajorityElectionInContestBund,
            EntryCandidates =
                {
                    new ProtoModels.MajorityElectionBallotGroupEntryCandidates
                    {
                        BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId21GossauMajorityElectionInContestBund,
                        CandidateIds =
                        {
                            MajorityElectionMockedData.CandidateId1GossauMajorityElectionInContestBund,
                        },
                    },
                    new ProtoModels.MajorityElectionBallotGroupEntryCandidates
                    {
                        BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId22GossauMajorityElectionInContestBund,
                        CandidateIds =
                        {
                            MajorityElectionMockedData.SecondaryElectionCandidateId1GossauMajorityElectionInContestBund,
                        },
                    },
                },
        });
    }

    [Fact]
    public async Task BallotGroupWithCandidateCountOkInPastContestShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.UpdateBallotGroupCandidatesAsync(new UpdateMajorityElectionBallotGroupCandidatesRequest
            {
                BallotGroupId = MajorityElectionMockedData.BallotGroupId1GossauMajorityElectionInContestBund,
                EntryCandidates =
                {
                        new ProtoModels.MajorityElectionBallotGroupEntryCandidates
                        {
                            BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId11GossauMajorityElectionInContestBund,
                            CandidateIds =
                            {
                                MajorityElectionMockedData.CandidateId1GossauMajorityElectionInContestBund,
                            },
                        },
                        new ProtoModels.MajorityElectionBallotGroupEntryCandidates
                        {
                            BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId12GossauMajorityElectionInContestBund,
                            CandidateIds =
                            {
                                MajorityElectionMockedData.SecondaryElectionCandidateId1GossauMajorityElectionInContestBund,
                            },
                        },
                },
            }),
            StatusCode.InvalidArgument,
            "The candidate count for this ballot group is correct, modifications aren't allowed anymore.");
    }

    [Fact]
    public async Task BallotGroupInLockedContestShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastLocked);
        await AssertStatus(
            async () => await AdminClient.UpdateBallotGroupCandidatesAsync(new UpdateMajorityElectionBallotGroupCandidatesRequest
            {
                BallotGroupId = MajorityElectionMockedData.BallotGroupId1GossauMajorityElectionInContestBund,
                EntryCandidates =
                {
                        new ProtoModels.MajorityElectionBallotGroupEntryCandidates
                        {
                            BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId11GossauMajorityElectionInContestBund,
                            CandidateIds =
                            {
                                MajorityElectionMockedData.CandidateId1GossauMajorityElectionInContestBund,
                            },
                        },
                        new ProtoModels.MajorityElectionBallotGroupEntryCandidates
                        {
                            BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId21GossauMajorityElectionInContestBund,
                            CandidateIds =
                            {
                                MajorityElectionMockedData.SecondaryElectionCandidateId1GossauMajorityElectionInContestBund,
                            },
                        },
                },
            }),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .UpdateBallotGroupCandidatesAsync(NewValidRequest());

    private UpdateMajorityElectionBallotGroupCandidatesRequest NewValidRequest(
        Action<UpdateMajorityElectionBallotGroupCandidatesRequest>? customizer = null)
    {
        var request = new UpdateMajorityElectionBallotGroupCandidatesRequest
        {
            BallotGroupId = MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund,
            EntryCandidates =
                {
                    new ProtoModels.MajorityElectionBallotGroupEntryCandidates
                    {
                        BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId1StGallenMajorityElectionInContestBund,
                        CandidateIds =
                        {
                            MajorityElectionMockedData.CandidateId2StGallenMajorityElectionInContestBund,
                        },
                    },
                    new ProtoModels.MajorityElectionBallotGroupEntryCandidates
                    {
                        BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId2StGallenMajorityElectionInContestBund,
                        CandidateIds =
                        {
                            MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund,
                        },
                    },
                },
        };

        customizer?.Invoke(request);
        return request;
    }
}
