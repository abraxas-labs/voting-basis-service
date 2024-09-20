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
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionCreateTest : BaseGrpcTest<ProportionalElectionService.ProportionalElectionServiceClient>
{
    public ProportionalElectionCreateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ProportionalElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task Test()
    {
        var response = await AdminClient.CreateAsync(NewValidRequest());

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionCreated, EventSignatureBusinessMetadata>();

        eventData.ProportionalElection.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.ProportionalElection.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.CreateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionCreated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id1 = Guid.Parse("f6ebc06e-a252-4cf4-9aa7-9ad46dd517f3");
        var id2 = Guid.Parse("53235b83-3dc3-4b72-a94d-871044168062");

        await TestEventPublisher.Publish(
            new ProportionalElectionCreated
            {
                ProportionalElection = new ProportionalElectionEventData
                {
                    Id = id1.ToString(),
                    PoliticalBusinessNumber = "6000",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Proporzwahl 1") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Proporzwahl 1") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                    ContestId = ContestMockedData.IdGossau,
                    NumberOfMandates = 6,
                    MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.HagenbachBischoff,
                    BallotNumberGeneration = SharedProto.BallotNumberGeneration.RestartForEachBundle,
                    ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Electronically,
                    EnforceReviewProcedureForCountingCircles = true,
                    CandidateCheckDigit = false,
                    EnforceCandidateCheckDigitForCountingCircles = true,
                    FederalIdentification = 29348929,
                },
            },
            new ProportionalElectionCreated
            {
                ProportionalElection = new ProportionalElectionEventData
                {
                    Id = id2.ToString(),
                    PoliticalBusinessNumber = "6001",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Proporzwahl 2") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Proporzwahl 2") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                    ContestId = ContestMockedData.IdBundContest,
                    NumberOfMandates = 3,
                    MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
                    BallotNumberGeneration = SharedProto.BallotNumberGeneration.RestartForEachBundle,
                    ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Physically,
                },
            });

        var proportionalElection1 = await AdminClient.GetAsync(new GetProportionalElectionRequest
        {
            Id = id1.ToString(),
        });
        var proportionalElection2 = await AdminClient.GetAsync(new GetProportionalElectionRequest
        {
            Id = id2.ToString(),
        });
        proportionalElection1.MatchSnapshot("1");
        proportionalElection2.MatchSnapshot("2");

        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusiness.HasEqualIdAndNewEntityState(id1, EntityState.Added));
        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusiness.HasEqualIdAndNewEntityState(id2, EntityState.Added));
    }

    [Theory]
    [InlineData(SharedProto.ProportionalElectionMandateAlgorithm.DoppelterPukelsheim0Quorum, SharedProto.ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum)]
    [InlineData(SharedProto.ProportionalElectionMandateAlgorithm.DoppelterPukelsheim5Quorum, SharedProto.ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum)]
    public async Task TestProcessorWithDeprecatedMandateAlgorithms(SharedProto.ProportionalElectionMandateAlgorithm deprecatedMandateAlgorithm, SharedProto.ProportionalElectionMandateAlgorithm expectedMandateAlgorithm)
    {
        var id = Guid.Parse("f6ebc06e-a252-4cf4-9aa7-9ad46dd517f3");
        await TestEventPublisher.Publish(
            new ProportionalElectionCreated
            {
                ProportionalElection = new ProportionalElectionEventData
                {
                    Id = id.ToString(),
                    PoliticalBusinessNumber = "6000",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Proporzwahl 1") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Proporzwahl 1") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                    ContestId = ContestMockedData.IdGossau,
                    NumberOfMandates = 6,
                    MandateAlgorithm = deprecatedMandateAlgorithm,
                    BallotNumberGeneration = SharedProto.BallotNumberGeneration.RestartForEachBundle,
                    ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Electronically,
                    EnforceReviewProcedureForCountingCircles = true,
                    CandidateCheckDigit = false,
                    EnforceCandidateCheckDigitForCountingCircles = true,
                },
            });

        var proportionalElection = await AdminClient.GetAsync(new GetProportionalElectionRequest
        {
            Id = id.ToString(),
        });
        proportionalElection.MandateAlgorithm.Should().Be(expectedMandateAlgorithm);
    }

    [Fact]
    public async Task ParentDoiWithSameTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(pe =>
             {
                 pe.ContestId = ContestMockedData.IdGossau;
                 pe.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen;
             })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ChildDoiWithSameTenantShouldReturnOk()
    {
        var response = await AdminClient.CreateAsync(NewValidRequest(pe =>
        {
            pe.ContestId = ContestMockedData.IdStGallenEvoting;
            pe.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau;
        }));

        var eventData = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionCreated>();

        eventData.ProportionalElection.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.ProportionalElection.Id);
    }

    [Fact]
    public async Task SiblingDoiWithSameTenantShouldThrow()
    {
        await AssertStatus(
              async () => await AdminClient.CreateAsync(NewValidRequest(pe =>
              {
                  pe.ContestId = ContestMockedData.IdStGallenEvoting;
                  pe.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdThurgau;
              })),
              StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ChildDoiWithDifferentTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(pe =>
            {
                pe.ContestId = ContestMockedData.IdStGallenEvoting;
                pe.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdUzwil;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task GreaterSampleSizeThanBallotSizeShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o => o.BallotBundleSampleSize = 9999)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ContinuousBallotNumberGenerationWithoutAutomaticGenerationShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o =>
            {
                o.AutomaticBallotBundleNumberGeneration = false;
                o.BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles;
            })),
            StatusCode.InvalidArgument,
            "BallotNumberGeneration");
    }

    [Fact]
    public async Task InvalidMandateAlgorithmByCantonShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o => o.MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ProportionalElectionInContestWithEndedTestingPhaseShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o =>
            {
                o.ContestId = ContestMockedData.IdPastUnlockedContest;
                o.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau;
            })),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    [Fact]
    public async Task VirtualTopLevelDomainOfInfluenceShouldThrow()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            x => x.Id == DomainOfInfluenceMockedData.GuidStGallen,
            x => x.VirtualTopLevel = true);

        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest()),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public Task DuplicatePoliticalBusinessIdShouldThrow()
    {
        return AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(v => v.PoliticalBusinessNumber = "155")),
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
        => await new ProportionalElectionService.ProportionalElectionServiceClient(channel)
            .CreateAsync(NewValidRequest());

    private CreateProportionalElectionRequest NewValidRequest(
        Action<CreateProportionalElectionRequest>? customizer = null)
    {
        var request = new CreateProportionalElectionRequest
        {
            PoliticalBusinessNumber = "4687",
            OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Proporzwahl") },
            ShortDescription = { LanguageUtil.MockAllLanguages("Proporzwahl") },
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
            ContestId = ContestMockedData.IdStGallenEvoting,
            Active = true,
            AutomaticBallotBundleNumberGeneration = true,
            AutomaticEmptyVoteCounting = true,
            BallotBundleSize = 13,
            BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
            CandidateCheckDigit = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.HagenbachBischoff,
            NumberOfMandates = 5,
            ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
            FederalIdentification = 29348929,
        };

        customizer?.Invoke(request);
        return request;
    }
}
