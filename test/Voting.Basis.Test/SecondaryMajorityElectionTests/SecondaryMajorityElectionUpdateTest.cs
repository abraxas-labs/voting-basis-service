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
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionUpdateTest : BaseGrpcTest<MajorityElectionService.MajorityElectionServiceClient>
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
        await AdminClient.UpdateSecondaryMajorityElectionAsync(NewValidRequest());

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
            await AdminClient.UpdateSecondaryMajorityElectionAsync(NewValidRequest());
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

        var majorityElection = await AdminClient.GetSecondaryMajorityElectionAsync(new GetSecondaryMajorityElectionRequest
        {
            Id = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
        });
        majorityElection.MatchSnapshot("event");

        var ballotGroup1 =
            await RunOnDb(db => db.MajorityElectionBallotGroups.Include(x => x.Entries).SingleAsync(x => x.Id == ballotGroupOkNowNotAfterUpdate));
        ballotGroup1.Entries.Single().CandidateCountOk.Should().BeFalse();

        var ballotGroup2 =
            await RunOnDb(db => db.MajorityElectionBallotGroups.Include(x => x.Entries).SingleAsync(x => x.Id == ballotGroupNotOkNowOkAfterUpdate));
        ballotGroup2.Entries.Single().CandidateCountOk.Should().BeTrue();

        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusiness.HasEqualIdAndNewEntityState(secondaryElectionId, EntityState.Modified));

        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.ElectionGroup.HasEqualIdAndNewEntityState(Guid.Parse(MajorityElectionMockedData.ElectionGroupIdStGallenMajorityElectionInContestBund), EntityState.Modified));
    }

    [Fact]
    public async Task MajorityElectionFromDifferentDoiShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateSecondaryMajorityElectionAsync(NewValidRequest(pe =>
            {
                pe.Id = MajorityElectionMockedData.SecondaryElectionIdKircheMajorityElectionInContestKirche;
                pe.PrimaryMajorityElectionId = MajorityElectionMockedData.IdKircheMajorityElectionInContestKirche;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task MajorityElectionIdChangeShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateSecondaryMajorityElectionAsync(NewValidRequest(o =>
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

        await AdminClient.UpdateSecondaryMajorityElectionAsync(new UpdateSecondaryMajorityElectionRequest
        {
            Id = id.ToString(),
            PrimaryMajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund,
            PoliticalBusinessNumber = "n1new",
            OfficialDescription = { LanguageUtil.MockAllLanguages("Nebenwahl Update") },
            ShortDescription = { LanguageUtil.MockAllLanguages("Nebenwahl Update") },
            NumberOfMandates = 3,
            AllowedCandidates = SharedProto.SecondaryMajorityElectionAllowedCandidates.MayExistInPrimaryElection,
        });

        var ev = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionAfterTestingPhaseUpdated>();
        ev.MatchSnapshot("event");

        await TestEventPublisher.Publish(ev);
        var election = await AdminClient.GetSecondaryMajorityElectionAsync(new GetSecondaryMajorityElectionRequest
        {
            Id = id.ToString(),
        });
        election.MatchSnapshot("reponse");

        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusiness.HasEqualIdAndNewEntityState(id, EntityState.Modified));
    }

    [Fact]
    public async Task SecondaryMajorityElectionUpdateAfterTestingPhaseShouldRestrictSomeFields()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.UpdateSecondaryMajorityElectionAsync(NewValidRequest(o =>
            {
                o.Id = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionInContestBund;
                o.PrimaryMajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund;
            })),
            StatusCode.FailedPrecondition,
            "ModificationNotAllowedException: Some modifications are not allowed because the testing phase has ended.");
    }

    [Fact]
    public async Task SecondaryMajorityElectionInLockedContestShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastLocked);
        await AssertStatus(
            async () => await AdminClient.UpdateSecondaryMajorityElectionAsync(NewValidRequest(o =>
            {
                o.Id = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionInContestBund;
                o.PrimaryMajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund;
            })),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
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
            AllowedCandidates = SharedProto.SecondaryMajorityElectionAllowedCandidates.MayExistInPrimaryElection,
            PrimaryMajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
            Active = true,
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
                AllowedCandidates = SharedProto.SecondaryMajorityElectionAllowedCandidates.MayExistInPrimaryElection,
                PrimaryMajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
                Active = true,
            },
        };

        customizer?.Invoke(ev);
        return ev;
    }
}
