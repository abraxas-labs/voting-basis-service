// (c) Copyright 2022 by Abraxas Informatik AG
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
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionActiveStateUpdateTest : BaseGrpcTest<MajorityElectionService.MajorityElectionServiceClient>
{
    public SecondaryMajorityElectionActiveStateUpdateTest(TestApplicationFactory factory)
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
        await AdminClient.UpdateSecondaryMajorityElectionActiveStateAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionActiveStateUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdBundContest);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdBundContest, async () =>
        {
            await AdminClient.UpdateSecondaryMajorityElectionActiveStateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<SecondaryMajorityElectionActiveStateUpdated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(
            new SecondaryMajorityElectionActiveStateUpdated
            {
                SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                Active = true,
            });
        var response = await AdminClient.GetSecondaryMajorityElectionAsync(new GetSecondaryMajorityElectionRequest { Id = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task SecondaryMajorityElectionFromOtherTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateSecondaryMajorityElectionActiveStateAsync(NewValidRequest(o =>
            {
                o.Id = MajorityElectionMockedData.SecondaryElectionIdKircheMajorityElectionInContestKirche;
            })),
            StatusCode.InvalidArgument);
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .UpdateSecondaryMajorityElectionActiveStateAsync(NewValidRequest());

    private UpdateSecondaryMajorityElectionActiveStateRequest NewValidRequest(
        Action<UpdateSecondaryMajorityElectionActiveStateRequest>? customizer = null)
    {
        var request = new UpdateSecondaryMajorityElectionActiveStateRequest
        {
            Id = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
            Active = true,
        };

        customizer?.Invoke(request);
        return request;
    }
}
