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

namespace Voting.Basis.Test.MajorityElectionTests;

public class MajorityElectionCreateTest : BaseGrpcTest<MajorityElectionService.MajorityElectionServiceClient>
{
    public MajorityElectionCreateTest(TestApplicationFactory factory)
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
        var response = await AdminClient.CreateAsync(NewValidRequest());

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCreated, EventSignatureBusinessMetadata>();

        eventData.MajorityElection.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.MajorityElection.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.CreateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionCreated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id1 = Guid.Parse("561592e7-b7dc-44d9-ac32-c1d11620c291");
        var id2 = Guid.Parse("a60c5616-c576-47d4-8ef4-4aa9b347ee60");

        await TestEventPublisher.Publish(
            new MajorityElectionCreated
            {
                MajorityElection = new MajorityElectionEventData
                {
                    Id = id1.ToString(),
                    PoliticalBusinessNumber = "8000",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Majorzwahl 1") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Neue Majorzwahl 1") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                    ContestId = ContestMockedData.IdGossau,
                    NumberOfMandates = 6,
                    MandateAlgorithm = SharedProto.MajorityElectionMandateAlgorithm.AbsoluteMajority,
                    ResultEntry = SharedProto.MajorityElectionResultEntry.FinalResults,
                    ReviewProcedure = SharedProto.MajorityElectionReviewProcedure.Electronically,
                    EnforceReviewProcedureForCountingCircles = true,
                    CandidateCheckDigit = true,
                    EnforceCandidateCheckDigitForCountingCircles = true,
                    IndividualCandidatesDisabled = true,
                    FederalIdentification = 92834984,
                },
            },
            new MajorityElectionCreated
            {
                MajorityElection = new MajorityElectionEventData
                {
                    Id = id2.ToString(),
                    PoliticalBusinessNumber = "8001",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Majorzwahl 2") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Neue Majorzwahl 2") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                    ContestId = ContestMockedData.IdBundContest,
                    NumberOfMandates = 3,
                    MandateAlgorithm = SharedProto.MajorityElectionMandateAlgorithm.RelativeMajority,
                    ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
                    ReviewProcedure = SharedProto.MajorityElectionReviewProcedure.Physically,
                },
            });

        var majorityElection1 = await AdminClient.GetAsync(new GetMajorityElectionRequest
        {
            Id = id1.ToString(),
        });
        var majorityElection2 = await AdminClient.GetAsync(new GetMajorityElectionRequest
        {
            Id = id2.ToString(),
        });
        majorityElection1.MatchSnapshot("1");
        majorityElection2.MatchSnapshot("2");

        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusiness.HasEqualIdAndNewEntityState(id1, EntityState.Added));
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
            pe.ReportDomainOfInfluenceLevel = 0;
        }));

        var eventData = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCreated>();

        eventData.MajorityElection.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.MajorityElection.Id);
    }

    [Fact]
    public async Task SiblingDoiWithSameTenantShouldThrow()
    {
        await AssertStatus(
              async () => await AdminClient.CreateAsync(NewValidRequest(pe =>
              {
                  pe.ContestId = ContestMockedData.IdStGallenEvoting;
                  pe.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdThurgau;
                  pe.ReportDomainOfInfluenceLevel = 0;
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
                pe.ReportDomainOfInfluenceLevel = 0;
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
    public async Task InvalidReportDomainOfInfluenceLevelShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o => o.ReportDomainOfInfluenceLevel = 13)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task MajorityElectionInContestWithEndedTestingPhaseShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.CreateAsync(NewValidRequest(o =>
            {
                o.ContestId = ContestMockedData.IdPastUnlockedContest;
                o.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau;
                o.ReportDomainOfInfluenceLevel = 0;
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
        => await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .CreateAsync(NewValidRequest());

    private CreateMajorityElectionRequest NewValidRequest(
        Action<CreateMajorityElectionRequest>? customizer = null)
    {
        var request = new CreateMajorityElectionRequest
        {
            PoliticalBusinessNumber = "9468",
            OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Majorzwahl") },
            ShortDescription = { LanguageUtil.MockAllLanguages("Neue Majorzwahl") },
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
            ContestId = ContestMockedData.IdStGallenEvoting,
            Active = true,
            AutomaticBallotBundleNumberGeneration = true,
            AutomaticEmptyVoteCounting = true,
            BallotBundleSize = 13,
            BallotBundleSampleSize = 1,
            BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
            CandidateCheckDigit = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            MandateAlgorithm = SharedProto.MajorityElectionMandateAlgorithm.AbsoluteMajority,
            ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
            EnforceResultEntryForCountingCircles = true,
            NumberOfMandates = 5,
            ReportDomainOfInfluenceLevel = 1,
            ReviewProcedure = SharedProto.MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
            IndividualCandidatesDisabled = true,
            FederalIdentification = 92834984,
        };

        customizer?.Invoke(request);
        return request;
    }
}
