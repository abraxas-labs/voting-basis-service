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
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionCreateTest : PoliticalBusinessAuthorizationGrpcBaseTest<MajorityElectionService.MajorityElectionServiceClient>
{
    public SecondaryMajorityElectionCreateTest(TestApplicationFactory factory)
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
        var response = await AdminClient.CreateSecondaryMajorityElectionAsync(NewValidRequest());

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionCreated, EventSignatureBusinessMetadata>();

        eventData.SecondaryMajorityElection.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.SecondaryMajorityElection.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.CreateSecondaryMajorityElectionAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<SecondaryMajorityElectionCreated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var electionGroupId = Guid.Parse("0f9f12b5-dbe7-436f-b78a-c6f389080cc0");
        var smeId1 = Guid.Parse("f0735502-3b24-40f2-9869-19023b67c6b8");
        var smeId2 = Guid.Parse("348f2f1a-063d-49ce-955a-81b464fcc256");

        await TestEventPublisher.Publish(
            new ElectionGroupCreated
            {
                ElectionGroup = new ElectionGroupEventData
                {
                    Id = electionGroupId.ToString(),
                    Number = 1,
                    Description = "test",
                    PrimaryMajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                },
            });
        await TestEventPublisher.Publish(
            1,
            new SecondaryMajorityElectionCreated
            {
                SecondaryMajorityElection = new SecondaryMajorityElectionEventData
                {
                    Id = smeId1.ToString(),
                    PoliticalBusinessNumber = "10226",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Neben-Majorzwahl") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Neue Neben-Majorzwahl") },
                    Active = true,
                    NumberOfMandates = 2,
                    AllowedCandidates = SharedProto.SecondaryMajorityElectionAllowedCandidates.MustExistInPrimaryElection,
                    PrimaryMajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                    IndividualCandidatesDisabled = true,
                },
            },
            new SecondaryMajorityElectionCreated
            {
                SecondaryMajorityElection = new SecondaryMajorityElectionEventData
                {
                    Id = smeId2.ToString(),
                    PoliticalBusinessNumber = "10286",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Neben-Majorzwahl 2") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Neue Neben-Majorzwahl 2") },
                    Active = false,
                    NumberOfMandates = 4,
                    AllowedCandidates = SharedProto.SecondaryMajorityElectionAllowedCandidates.MustNotExistInPrimaryElection,
                    PrimaryMajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                },
            });

        var secondaryMajorityElection1 = await AdminClient.GetSecondaryMajorityElectionAsync(new GetSecondaryMajorityElectionRequest
        {
            Id = smeId1.ToString(),
        });
        var secondaryMajorityElection2 = await AdminClient.GetSecondaryMajorityElectionAsync(new GetSecondaryMajorityElectionRequest
        {
            Id = smeId2.ToString(),
        });
        secondaryMajorityElection1.MatchSnapshot("1");
        secondaryMajorityElection2.MatchSnapshot("2");

        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.ElectionGroup.HasEqualIdAndNewEntityState(electionGroupId, EntityState.Added));
        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusiness.HasEqualIdAndNewEntityState(smeId1, EntityState.Added));
        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusiness.HasEqualIdAndNewEntityState(smeId2, EntityState.Added));

        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.ElectionGroup.HasEqualIdAndNewEntityState(electionGroupId, EntityState.Modified));
    }

    [Fact]
    public async Task CreatingSecondaryElectionShouldCreateElectionGroupIfNoneExists()
    {
        var request = NewValidRequest();
        await AdminClient.CreateSecondaryMajorityElectionAsync(request);

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ElectionGroupCreated, EventSignatureBusinessMetadata>();
        var createdElectionGroup = eventData.ElectionGroup;
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);

        // this should be the second election group, since one already exists in this contest
        createdElectionGroup.Description.Should().Be("Wahlgruppe 2");
        createdElectionGroup.Number.Should().Be(2);
        createdElectionGroup.PrimaryMajorityElectionId.Should().Be(request.PrimaryMajorityElectionId);
    }

    [Fact]
    public Task DuplicatePoliticalBusinessIdShouldThrow()
    {
        return AssertStatus(
            async () => await AdminClient.CreateSecondaryMajorityElectionAsync(NewValidRequest(v => v.PoliticalBusinessNumber = "155")),
            StatusCode.AlreadyExists);
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .CreateSecondaryMajorityElectionAsync(NewValidRequest());

    private CreateSecondaryMajorityElectionRequest NewValidRequest(
        Action<CreateSecondaryMajorityElectionRequest>? customizer = null)
    {
        var request = new CreateSecondaryMajorityElectionRequest
        {
            PoliticalBusinessNumber = "10246",
            OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Neben-Majorzwahl") },
            ShortDescription = { LanguageUtil.MockAllLanguages("Neue Neben-Majorzwahl") },
            Active = true,
            NumberOfMandates = 5,
            AllowedCandidates = SharedProto.SecondaryMajorityElectionAllowedCandidates.MayExistInPrimaryElection,
            PrimaryMajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
            IndividualCandidatesDisabled = true,
        };

        customizer?.Invoke(request);
        return request;
    }
}
