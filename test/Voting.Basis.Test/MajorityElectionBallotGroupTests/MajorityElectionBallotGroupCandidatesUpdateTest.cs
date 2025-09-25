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
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.MajorityElectionBallotGroupTests;

public class MajorityElectionBallotGroupCandidatesUpdateTest : PoliticalBusinessAuthorizationGrpcBaseTest<MajorityElectionService.MajorityElectionServiceClient>
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
        await ElectionAdminClient.UpdateBallotGroupCandidatesAsync(request);

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionBallotGroupCandidatesUpdated, EventSignatureBusinessMetadata>();

        eventData.BallotGroupCandidates.BallotGroupId.Should().Be(request.BallotGroupId);
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdBundContest);
    }

    [Fact]
    public async Task TestWithIndividualCandidate()
    {
        await ElectionAdminClient.UpdateBallotGroupCandidatesAsync(NewValidRequest(x =>
        {
            x.EntryCandidates[0].CandidateIds.Clear();
            x.EntryCandidates[0].IndividualCandidatesVoteCount = 1;
        }));
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
                            BlankRowCount = 0,
                        },
                        new MajorityElectionBallotGroupEntryCandidatesEventData
                        {
                            BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId2StGallenMajorityElectionInContestBund,
                            IndividualCandidatesVoteCount = 2,
                            BlankRowCount = 1,
                        },
                    },
            },
        };

        await TestEventPublisher.Publish(ev);

        var ballotGroupCandidates = await ElectionAdminClient.ListBallotGroupCandidatesAsync(new ListMajorityElectionBallotGroupCandidatesRequest
        {
            BallotGroupId = MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund,
        });

        ballotGroupCandidates.MatchSnapshot();

        var ballotGroups = await ElectionAdminClient.ListBallotGroupsAsync(new ListMajorityElectionBallotGroupsRequest
        {
            MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
        });

        var ballotGroupEntry1 = ballotGroups.BallotGroups
            .Single(x => x.Id == MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund)
            .Entries
            .Single(x => x.Id == MajorityElectionMockedData.BallotGroupEntryId1StGallenMajorityElectionInContestBund);
        ballotGroupEntry1.CountOfCandidates.Should().Be(1);
        ballotGroupEntry1.IndividualCandidatesVoteCount.Should().Be(0);
        ballotGroupEntry1.CandidateCountOk.Should().BeTrue();

        var ballotGroupEntry2 = ballotGroups.BallotGroups
            .Single(x => x.Id == MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund)
            .Entries
            .Single(x => x.Id == MajorityElectionMockedData.BallotGroupEntryId2StGallenMajorityElectionInContestBund);
        ballotGroupEntry2.CountOfCandidates.Should().Be(0);
        ballotGroupEntry2.IndividualCandidatesVoteCount.Should().Be(2);
        ballotGroupEntry2.BlankRowCount.Should().Be(1);
        ballotGroupEntry2.CandidateCountOk.Should().BeTrue();
    }

    [Fact]
    public async Task TestProcessorDeprecatedEventWithNoBlankRowCount()
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
                            IndividualCandidatesVoteCount = 1,
                        },
                    },
            },
        };

        await TestEventPublisher.Publish(ev);

        var ballotGroups = await ElectionAdminClient.ListBallotGroupsAsync(new ListMajorityElectionBallotGroupsRequest
        {
            MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
        });

        var ballotGroupEntry2 = ballotGroups.BallotGroups
            .Single(x => x.Id == MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund)
            .Entries
            .Single(x => x.Id == MajorityElectionMockedData.BallotGroupEntryId2StGallenMajorityElectionInContestBund);

        // Blank row count should remain unchanged
        ballotGroupEntry2.BlankRowCount.Should().Be(2);
    }

    [Fact]
    public async Task MissingEntriesShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateBallotGroupCandidatesAsync(NewValidRequest(o => o.EntryCandidates.Clear())),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task DuplicateEntriesShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateBallotGroupCandidatesAsync(NewValidRequest(o =>
            o.EntryCandidates.Add(o.EntryCandidates[0]))),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task BallotGroupWithCandidateCountNotOkShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateBallotGroupCandidatesAsync(new UpdateMajorityElectionBallotGroupCandidatesRequest
            {
                BallotGroupId = MajorityElectionMockedData.BallotGroupId2GossauMajorityElectionInContestBund,
                EntryCandidates =
                {
                    new ProtoModels.MajorityElectionBallotGroupEntryCandidates
                    {
                        BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId21GossauMajorityElectionInContestBund,
                        IndividualCandidatesVoteCount = 0,
                        CandidateIds =
                        {
                            MajorityElectionMockedData.CandidateId1GossauMajorityElectionInContestBund,
                        },
                    },
                    new ProtoModels.MajorityElectionBallotGroupEntryCandidates
                    {
                        BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId22GossauMajorityElectionInContestBund,
                        IndividualCandidatesVoteCount = 0,
                        CandidateIds =
                        {
                            MajorityElectionMockedData.SecondaryElectionCandidateId1GossauMajorityElectionInContestBund,
                        },
                    },
                },
            }),
            StatusCode.FailedPrecondition,
            nameof(MajorityElectionBallotGroupVoteCountException));
    }

    [Fact]
    public async Task BallotGroupWithExistingCandidateCountOkInPastContestShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateBallotGroupCandidatesAsync(new UpdateMajorityElectionBallotGroupCandidatesRequest
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
            async () => await ElectionAdminClient.UpdateBallotGroupCandidatesAsync(new UpdateMajorityElectionBallotGroupCandidatesRequest
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
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task ModificationWithEVotingApprovedShouldWork()
    {
        await ElectionAdminClient.UpdateBallotGroupCandidatesAsync(new UpdateMajorityElectionBallotGroupCandidatesRequest
        {
            BallotGroupId = MajorityElectionMockedData.BallotGroupIdGossauMajorityElectionEVotingApprovedInContestBund,
            EntryCandidates =
            {
                new ProtoModels.MajorityElectionBallotGroupEntryCandidates
                {
                    BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId1GossauMajorityElectionEVotingApprovedInContestBund,
                    CandidateIds =
                    {
                        MajorityElectionMockedData.CandidateIdGossauMajorityElectionEVotingApprovedInContestStGallen,
                    },
                    IndividualCandidatesVoteCount = 1,
                    BlankRowCount = 1,
                },
                new ProtoModels.MajorityElectionBallotGroupEntryCandidates
                {
                    BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId2GossauMajorityElectionEVotingApprovedInContestBund,
                    BlankRowCount = 3,
                },
            },
        });

        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionBallotGroupCandidatesUpdated>()
            .Should().NotBeNull();
    }

    [Fact]
    public async Task SetNonZeroIndividualVotesOnElectionWithDisableIndividualVotesShouldThrow()
    {
        await ModifyDbEntities<MajorityElection>(
            x => x.Id == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund),
            x => x.IndividualCandidatesDisabled = true);

        await AssertStatus(
            async () => await ElectionAdminClient.UpdateBallotGroupCandidatesAsync(NewValidRequest(x => x.EntryCandidates[0].IndividualCandidatesVoteCount = 1)),
            StatusCode.InvalidArgument,
            "Individual candidates vote count not enabled on ballot group entry");
    }

    [Fact]
    public async Task SetOnlyBlankRowsShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateBallotGroupCandidatesAsync(new()
            {
                BallotGroupId = MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund,
                EntryCandidates =
                {
                    new ProtoModels.MajorityElectionBallotGroupEntryCandidates
                    {
                        BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId1StGallenMajorityElectionInContestBund,
                        BlankRowCount = 1,
                    },
                    new ProtoModels.MajorityElectionBallotGroupEntryCandidates
                    {
                        BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId2StGallenMajorityElectionInContestBund,
                        BlankRowCount = 3,
                    },
                },
            }),
            StatusCode.InvalidArgument,
            "A ballot group cannot contain only blank rows");
    }

    [Fact]
    public async Task SetBlankRowCountOnASingleMandateElectionWithoutOtherElectionsOnSameBallotShouldThrow()
    {
        // Ensure that it is a single mandate election without other elections on the same ballot.
        await ElectionAdminClient.DeleteSecondaryMajorityElectionAsync(new()
        {
            Id = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionInContestBund,
        });

        await AssertStatus(
            async () => await ElectionAdminClient.UpdateBallotGroupCandidatesAsync(new()
            {
                BallotGroupId = MajorityElectionMockedData.BallotGroupId1GossauMajorityElectionInContestBund,
                EntryCandidates =
                {
                    new ProtoModels.MajorityElectionBallotGroupEntryCandidates
                    {
                        BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId11GossauMajorityElectionInContestBund,
                        BlankRowCount = 1,
                    },
                },
            }),
            StatusCode.InvalidArgument,
            "Cannot set blank row count on a single mandate election without secondary elections on the same ballot");
    }

    [Fact]
    public async Task SecondaryCandidateWithReferenceNotSelectedInPrimaryElectionShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateBallotGroupCandidatesAsync(new()
            {
                BallotGroupId = MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund,
                EntryCandidates =
                {
                    new ProtoModels.MajorityElectionBallotGroupEntryCandidates
                    {
                        BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId1StGallenMajorityElectionInContestBund,
                        CandidateIds =
                        {
                            MajorityElectionMockedData.CandidateId2BundMajorityElectionInContestStGallen,
                        },
                    },
                    new ProtoModels.MajorityElectionBallotGroupEntryCandidates
                    {
                        BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId2StGallenMajorityElectionInContestBund,
                        CandidateIds =
                        {
                            MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund,
                        },
                        BlankRowCount = 2,
                    },
                },
            }),
            StatusCode.FailedPrecondition,
            nameof(SecondaryMajorityElectionCandidateNotSelectedInPrimaryElectionException));
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
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
                        BlankRowCount = 2,
                    },
                },
        };

        customizer?.Invoke(request);
        return request;
    }
}
