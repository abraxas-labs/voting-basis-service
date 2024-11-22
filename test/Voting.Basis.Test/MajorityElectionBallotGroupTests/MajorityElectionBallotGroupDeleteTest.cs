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
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.MajorityElectionBallotGroupTests;

public class MajorityElectionBallotGroupDeleteTest : PoliticalBusinessAuthorizationGrpcBaseTest<MajorityElectionService.MajorityElectionServiceClient>
{
    private const string IdNotFound = "bfe2cfaf-c787-48b9-a108-c975b0addddd";
    private string? _authTestBallotGroupId;

    public MajorityElectionBallotGroupDeleteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await MajorityElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestNotFound()
    {
        await AssertStatus(
            async () => await AdminClient.DeleteBallotGroupAsync(new DeleteMajorityElectionBallotGroupRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task Test()
    {
        await AdminClient.DeleteBallotGroupAsync(new DeleteMajorityElectionBallotGroupRequest
        {
            Id = MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund,
        });
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionBallotGroupDeleted, EventSignatureBusinessMetadata>();

        eventData.BallotGroupId.Should().Be(MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund);
        eventData.MatchSnapshot();
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdBundContest);
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund;
        await TestEventPublisher.Publish(new MajorityElectionBallotGroupDeleted { BallotGroupId = id });

        var idGuid = Guid.Parse(id);
        (await RunOnDb(db => db.MajorityElectionBallotGroups.CountAsync(c => c.Id == idGuid)))
            .Should().Be(0);
    }

    [Fact]
    public async Task BallotGroupInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.DeleteBallotGroupAsync(new DeleteMajorityElectionBallotGroupRequest
            {
                Id = MajorityElectionMockedData.BallotGroupId1GossauMajorityElectionInContestBund,
            }),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        if (_authTestBallotGroupId == null)
        {
            var response = await ElectionAdminClient.CreateBallotGroupAsync(new CreateMajorityElectionBallotGroupRequest
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
            });
            await RunEvents<MajorityElectionBallotGroupCreated>();

            _authTestBallotGroupId = response.Id;
        }

        await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .DeleteBallotGroupAsync(new DeleteMajorityElectionBallotGroupRequest { Id = _authTestBallotGroupId });
        _authTestBallotGroupId = null;
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }
}
