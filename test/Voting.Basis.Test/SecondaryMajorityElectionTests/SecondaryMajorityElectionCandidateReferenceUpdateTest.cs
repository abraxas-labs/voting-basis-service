// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
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
using Voting.Basis.Core.Utils;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionCandidateReferenceUpdateTest : PoliticalBusinessAuthorizationGrpcBaseTest<MajorityElectionService.MajorityElectionServiceClient>
{
    public SecondaryMajorityElectionCandidateReferenceUpdateTest(TestApplicationFactory factory)
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
        await CantonAdminClient.UpdateMajorityElectionCandidateReferenceAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionCandidateReferenceUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdBundContest);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdBundContest, async () =>
        {
            await CantonAdminClient.UpdateMajorityElectionCandidateReferenceAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<SecondaryMajorityElectionCandidateReferenceUpdated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(
            new SecondaryMajorityElectionCandidateReferenceUpdated
            {
                MajorityElectionCandidateReference = new MajorityElectionCandidateReferenceEventData
                {
                    Id = MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund,
                    SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                    CandidateId = MajorityElectionMockedData.CandidateId1StGallenMajorityElectionInContestBund,
                    Incumbent = true,
                    Position = 1,
                    Number = "2.1",
                    CheckDigit = ElectionCandidateCheckDigitUtils.CalculateCheckDigit("2.1"),
                },
            });

        var candidates = await CantonAdminClient.ListSecondaryMajorityElectionCandidatesAsync(new ListSecondaryMajorityElectionCandidatesRequest
        {
            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
        });
        candidates.MatchSnapshot();
    }

    [Fact]
    public async Task NonContinuousPositionShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateMajorityElectionCandidateReferenceAsync(NewValidRequest(o =>
            {
                o.Position = 5;
            })),
            StatusCode.InvalidArgument);
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .UpdateMajorityElectionCandidateReferenceAsync(NewValidRequest());

    private UpdateMajorityElectionCandidateReferenceRequest NewValidRequest(
        Action<UpdateMajorityElectionCandidateReferenceRequest>? customizer = null)
    {
        var request = new UpdateMajorityElectionCandidateReferenceRequest
        {
            Id = MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund,
            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
            CandidateId = MajorityElectionMockedData.CandidateId1StGallenMajorityElectionInContestBund,
            Incumbent = true,
            Number = "3.2",
            Position = 1,
        };

        customizer?.Invoke(request);
        return request;
    }
}
