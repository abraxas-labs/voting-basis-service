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
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.MajorityElectionUnionTests;

public class MajorityElectionUnionUpdateTest : PoliticalBusinessUnionAuthorizationGrpcBaseTest<MajorityElectionUnionService.MajorityElectionUnionServiceClient>
{
    public MajorityElectionUnionUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await MajorityElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestShouldReturnOk()
    {
        await CantonAdminClient.UpdateAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionUnionUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await CantonAdminClient.UpdateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionUnionUpdated>();
        });
    }

    [Fact]
    public async Task TestProcessor()
    {
        var id = Guid.Parse(MajorityElectionUnionMockedData.IdStGallen1);

        await TestEventPublisher.Publish(
            new MajorityElectionUnionUpdated
            {
                MajorityElectionUnion = new MajorityElectionUnionEventData
                {
                    Id = id.ToString(),
                    Description = "edited description",
                    ContestId = ContestMockedData.IdStGallenEvoting,
                },
            });

        var result = await RunOnDb(db => db.MajorityElectionUnions
            .Include(u => u.MajorityElectionUnionEntries.OrderBy(m => m.Id))
            .FirstOrDefaultAsync(u => u.Id == id));
        result.MatchSnapshot();

        await AssertHasPublishedEventProcessedMessage(MajorityElectionUnionUpdated.Descriptor, id);
    }

    [Fact]
    public async Task InvalidIdShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateAsync(NewValidRequest(x => x.Id = "b4e22024-113b-49ac-8460-2bf1c4a074b1")),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task ContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionUnionService.MajorityElectionUnionServiceClient(channel)
            .UpdateAsync(NewValidRequest());
    }

    private UpdateMajorityElectionUnionRequest NewValidRequest(
        Action<UpdateMajorityElectionUnionRequest>? customizer = null)
    {
        var request = new UpdateMajorityElectionUnionRequest
        {
            Id = MajorityElectionUnionMockedData.IdStGallen1,
            Description = "edited description",
        };

        customizer?.Invoke(request);
        return request;
    }
}
