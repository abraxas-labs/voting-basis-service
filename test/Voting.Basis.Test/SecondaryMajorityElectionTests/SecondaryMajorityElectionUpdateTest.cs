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
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionUpdateTest : PoliticalBusinessAuthorizationGrpcBaseTest<MajorityElectionService.MajorityElectionServiceClient>
{
    public SecondaryMajorityElectionUpdateTest(TestApplicationFactory factory)
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
        await CantonAdminClient.UpdateSecondaryMajorityElectionAsync(NewValidRequest());

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionUpdated, EventSignatureBusinessMetadata>();

        eventData.SecondaryMajorityElection.Id.Should().Be(request.Id);
        eventData.MatchSnapshot("event", d => d.SecondaryMajorityElection.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdBundContest);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdBundContest, async () =>
        {
            await CantonAdminClient.UpdateSecondaryMajorityElectionAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<SecondaryMajorityElectionUpdated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var primaryElectionId = Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen);
        var secondaryElectionId = Guid.Parse(MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund);
        var ballotGroupOkNowNotAfterUpdate = Guid.Parse("a66ed057-61c2-4239-a2cf-ae4bf0ecd6d4");
        var ballotGroupNotOkNowOkAfterUpdate = Guid.Parse("c7d64f82-500c-4d07-b215-efb7d32e86e3");
        await RunOnDb(db =>
        {
            db.MajorityElectionBallotGroups.AddRange(
                new MajorityElectionBallotGroup
                {
                    Id = ballotGroupOkNowNotAfterUpdate,
                    Description = "okNowNotAfterUpdate",
                    Position = 1,
                    ShortDescription = "okNowNotAfterUpdate",
                    MajorityElectionId = primaryElectionId,
                    Entries = new List<MajorityElectionBallotGroupEntry>
                    {
                            new MajorityElectionBallotGroupEntry
                            {
                                BlankRowCount = 1,
                                CountOfCandidates = 1,
                                IndividualCandidatesVoteCount = 1,
                                SecondaryMajorityElectionId = secondaryElectionId,
                            },
                    },
                },
                new MajorityElectionBallotGroup
                {
                    Id = ballotGroupNotOkNowOkAfterUpdate,
                    Description = "notOkNowOkAfterUpdate",
                    Position = 2,
                    ShortDescription = "notOkNowOkAfterUpdate",
                    MajorityElectionId = primaryElectionId,
                    Entries = new List<MajorityElectionBallotGroupEntry>
                    {
                            new MajorityElectionBallotGroupEntry
                            {
                                BlankRowCount = 1,
                                CountOfCandidates = 1,
                                IndividualCandidatesVoteCount = 0,
                                SecondaryMajorityElectionId = secondaryElectionId,
                            },
                    },
                });

            return db.SaveChangesAsync();
        });

        await TestEventPublisher.Publish(NewValidEvent());

        var majorityElection = await CantonAdminClient.GetSecondaryMajorityElectionAsync(new GetSecondaryMajorityElectionRequest
        {
            Id = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
        });
        majorityElection.MatchSnapshot("event");

        var simplePb = await RunOnDb(db => db.SimplePoliticalBusiness.SingleAsync(x => x.Id == secondaryElectionId));
        simplePb.MatchSnapshot("simple-political-business");

        var ballotGroup1 =
            await RunOnDb(db => db.MajorityElectionBallotGroups.Include(x => x.Entries).SingleAsync(x => x.Id == ballotGroupOkNowNotAfterUpdate));
        ballotGroup1.Entries.Single().CandidateCountOk.Should().BeFalse();

        var ballotGroup2 =
            await RunOnDb(db => db.MajorityElectionBallotGroups.Include(x => x.Entries).SingleAsync(x => x.Id == ballotGroupNotOkNowOkAfterUpdate));
        ballotGroup2.Entries.Single().CandidateCountOk.Should().BeTrue();

        await AssertHasPublishedEventProcessedMessage(SecondaryMajorityElectionUpdated.Descriptor, secondaryElectionId);
    }

    [Fact]
    public async Task MajorityElectionIdChangeShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateSecondaryMajorityElectionAsync(NewValidRequest(o =>
            {
                o.PrimaryMajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestGossau;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task SecondaryMajorityElectionUpdateAfterTestingPhaseShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        var id = Guid.Parse(MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionInContestBund);

        await CantonAdminClient.UpdateSecondaryMajorityElectionAsync(new UpdateSecondaryMajorityElectionRequest
        {
            Id = id.ToString(),
            PrimaryMajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund,
            PoliticalBusinessNumber = "n1new",
            OfficialDescription = { LanguageUtil.MockAllLanguages("Nebenwahl Update") },
            ShortDescription = { LanguageUtil.MockAllLanguages("Nebenwahl Update") },
            NumberOfMandates = 3,
        });

        var ev = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionAfterTestingPhaseUpdated>();
        ev.MatchSnapshot("event");

        await TestEventPublisher.Publish(ev);
        var election = await CantonAdminClient.GetSecondaryMajorityElectionAsync(new GetSecondaryMajorityElectionRequest
        {
            Id = id.ToString(),
        });
        election.MatchSnapshot("reponse");

        await AssertHasPublishedEventProcessedMessage(SecondaryMajorityElectionAfterTestingPhaseUpdated.Descriptor, id);
    }

    [Fact]
    public async Task SecondaryMajorityElectionUpdateAfterTestingPhaseShouldRestrictSomeFields()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await CantonAdminClient.UpdateSecondaryMajorityElectionAsync(NewValidRequest(o =>
            {
                o.Id = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionInContestBund;
                o.PrimaryMajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund;
                o.IndividualCandidatesDisabled = false;
            })),
            StatusCode.FailedPrecondition,
            "ModificationNotAllowedException: Some modifications are not allowed because the testing phase has ended.");
    }

    [Fact]
    public async Task SecondaryMajorityElectionInLockedContestShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastLocked);
        await AssertStatus(
            async () => await CantonAdminClient.UpdateSecondaryMajorityElectionAsync(NewValidRequest(o =>
            {
                o.Id = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionInContestBund;
                o.PrimaryMajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund;
            })),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public Task DuplicatePoliticalBusinessIdShouldThrow()
    {
        return AssertStatus(
            async () => await CantonAdminClient.UpdateSecondaryMajorityElectionAsync(NewValidRequest(v => v.PoliticalBusinessNumber = "n2")),
            StatusCode.AlreadyExists);
    }

    [Fact]
    public Task DuplicateMajorityElectionPoliticalBusinessIdShouldThrow()
    {
        return AssertStatus(
            async () => await CantonAdminClient.UpdateSecondaryMajorityElectionAsync(NewValidRequest(v => v.PoliticalBusinessNumber = "201")),
            StatusCode.AlreadyExists);
    }

    [Fact]
    public async Task DisableIndividualVotesWithExistingBallotGroupIndividualVotesShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminUzwilClient.UpdateSecondaryMajorityElectionAsync(NewValidRequest(x =>
            {
                x.Id = MajorityElectionMockedData.SecondaryElectionIdUzwilMajorityElectionInContestStGallen;
                x.PrimaryMajorityElectionId = MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen;
            })),
            StatusCode.InvalidArgument,
            "Cannot disable individual candidates when there are individual candidates vote count defined on ballot group entries");
    }

    [Fact]
    public async Task UpdateNumberOfMandatesOnActiveElectionShouldThrow()
    {
        var req = NewValidRequest(v => v.NumberOfMandates = 2);

        await ElectionAdminClient.UpdateSecondaryMajorityElectionActiveStateAsync(new()
        {
            Id = req.Id,
            Active = true,
        });

        await AssertStatus(
            async () => await ElectionAdminClient.UpdateSecondaryMajorityElectionAsync(req),
            StatusCode.FailedPrecondition,
            nameof(MajorityElectionActiveNumberOfMandatesChangeException));
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .UpdateSecondaryMajorityElectionAsync(NewValidRequest());

    private UpdateSecondaryMajorityElectionRequest NewValidRequest(
        Action<UpdateSecondaryMajorityElectionRequest>? customizer = null)
    {
        var request = new UpdateSecondaryMajorityElectionRequest
        {
            Id = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
            PoliticalBusinessNumber = "10546",
            OfficialDescription = { LanguageUtil.MockAllLanguages("Update Nebenwahl") },
            ShortDescription = { LanguageUtil.MockAllLanguages("Update Nebenwahl") },
            NumberOfMandates = 2,
            PrimaryMajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
            Active = true,
            IndividualCandidatesDisabled = true,
        };

        customizer?.Invoke(request);
        return request;
    }

    private SecondaryMajorityElectionUpdated NewValidEvent(
        Action<SecondaryMajorityElectionUpdated>? customizer = null)
    {
        var ev = new SecondaryMajorityElectionUpdated
        {
            SecondaryMajorityElection = new SecondaryMajorityElectionEventData
            {
                Id = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                PoliticalBusinessNumber = "10546",
                OfficialDescription = { LanguageUtil.MockAllLanguages("Update Nebenwahl") },
                ShortDescription = { LanguageUtil.MockAllLanguages("Update Nebenwahl") },
                NumberOfMandates = 2,
                PrimaryMajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
                Active = true,
                IndividualCandidatesDisabled = true,
            },
        };

        customizer?.Invoke(ev);
        return ev;
    }
}
