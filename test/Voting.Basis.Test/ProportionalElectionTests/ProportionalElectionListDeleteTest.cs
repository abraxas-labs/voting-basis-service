// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
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
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionListDeleteTest : BaseGrpcTest<ProportionalElectionService.ProportionalElectionServiceClient>
{
    private const string IdNotFound = "bfe2cfaf-c787-48b9-a108-c975b0addddd";
    private string? _authTestListId;

    public ProportionalElectionListDeleteTest(TestApplicationFactory factory)
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
            async () => await AdminClient.DeleteListAsync(new DeleteProportionalElectionListRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task Test()
    {
        await AdminClient.DeleteListAsync(new DeleteProportionalElectionListRequest
        {
            Id = ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen,
        });
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListDeleted, EventSignatureBusinessMetadata>();

        eventData.ProportionalElectionListId.Should().Be(ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen);
        eventData.MatchSnapshot();
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.DeleteListAsync(new DeleteProportionalElectionListRequest
            {
                Id = ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen,
            });
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionListDeleted>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen;
        await TestEventPublisher.Publish(new ProportionalElectionListDeleted { ProportionalElectionListId = id });

        var idGuid = Guid.Parse(id);
        (await RunOnDb(db => db.ProportionalElectionLists.CountAsync(c => c.Id == idGuid)))
            .Should().Be(0);

        await AssertHasPublishedMessage<ProportionalElectionListChangeMessage>(
            x => x.List.HasEqualIdAndNewEntityState(idGuid, EntityState.Deleted));
    }

    [Fact]
    public async Task TestAggregateUnionLists()
    {
        var id = ProportionalElectionMockedData.ListId3GossauProportionalElectionInContestStGallen;
        await TestEventPublisher.Publish(new ProportionalElectionListDeleted { ProportionalElectionListId = id });

        (await RunOnDb(db =>
            db.ProportionalElectionUnionLists
                .Where(l => ProportionalElectionUnionMockedData.StGallen1.Id == l.ProportionalElectionUnionId)
                .Select(l => new { l.OrderNumber, l.ShortDescription })
                .OrderBy(x => x.OrderNumber)
                .ToListAsync())).MatchSnapshot();
    }

    [Fact]
    public async Task ProportionalElectionOtherTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.DeleteListAsync(new DeleteProportionalElectionListRequest
            {
                Id = ProportionalElectionMockedData.ListIdUzwilProportionalElectionInContestStGallen,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestParentProportionalElectionShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteListAsync(new DeleteProportionalElectionListRequest
            {
                Id = ProportionalElectionMockedData.ListIdBundProportionalElectionInContestStGallen,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ProportionalElectionInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteListAsync(new DeleteProportionalElectionListRequest
            {
                Id = ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestBund,
            }),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        if (_authTestListId == null)
        {
            var response = await ElectionAdminClient.CreateListAsync(new CreateProportionalElectionListRequest
            {
                BlankRowCount = 0,
                OrderNumber = "o1",
                Position = 1,
                ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallenWithoutChilds,
                Description = { LanguageUtil.MockAllLanguages("Created list") },
                ShortDescription = { LanguageUtil.MockAllLanguages("Juso") },
            });
            await RunEvents<ProportionalElectionListCreated>();

            _authTestListId = response.Id;
        }

        await new ProportionalElectionService.ProportionalElectionServiceClient(channel)
            .DeleteListAsync(new DeleteProportionalElectionListRequest { Id = _authTestListId });
        _authTestListId = null;
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }
}
