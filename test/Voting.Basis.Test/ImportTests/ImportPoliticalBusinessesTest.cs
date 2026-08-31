// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Basis.Core.Auth;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.ImportTests.TestFiles;
using Voting.Basis.Test.MockedData;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.ImportTests;

public class ImportPoliticalBusinessesTest : BaseImportPoliticalBusinessAuthorizationTest
{
    public ImportPoliticalBusinessesTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await ContestMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped, false);
        await ProportionalElectionMockedData.Seed(RunScoped, false);
        await VoteMockedData.Seed(RunScoped, false);
    }

    [Fact]
    public async Task TestShouldWork()
    {
        var request = await CreateValidRequest();
        await CantonAdminClient.ImportPoliticalBusinessesAsync(request);
    }

    [Fact]
    public async Task TestWithV5ShouldWork()
    {
        var request = await CreateValidRequest(v5: true);
        await CantonAdminClient.ImportPoliticalBusinessesAsync(request);
    }

    [Fact]
    public async Task TestShouldEnableEVotingApproval()
    {
        var request = await CreateValidRequest(contestId: ContestMockedData.IdStGallenEvoting);
        request.Votes[0].Vote.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdUzwil;
        request.MajorityElections[0].Election.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdUzwil;

        await CantonAdminClient.ImportPoliticalBusinessesAsync(request);

        var peCreatedEvents = EventPublisherMock.GetPublishedEvents<ProportionalElectionCreated>();
        var meCreatedEvents = EventPublisherMock.GetPublishedEvents<MajorityElectionCreated>();
        var smeCreatedEvents = EventPublisherMock.GetPublishedEvents<SecondaryMajorityElectionCreated>();
        var voteCreatedEvents = EventPublisherMock.GetPublishedEvents<VoteCreated>();

        voteCreatedEvents.First().Vote.EVotingApproved.Should().BeNull();
        meCreatedEvents.First().MajorityElection.EVotingApproved.Should().BeNull();
        smeCreatedEvents.First().SecondaryMajorityElection.EVotingApproved.Should().BeNull();
        voteCreatedEvents.Any(v => v.Vote.EVotingApproved == false).Should().BeTrue();
        peCreatedEvents.Any(pe => pe.ProportionalElection.EVotingApproved == false).Should().BeTrue();
    }

    [Fact]
    public async Task TestAllTypesOfVotesShouldWork()
    {
        var contest = await LoadContestImport(SharedProto.ImportType.Ech159, EchTestFiles.GetTestFilePath(EchTestFiles.Ech0159AllTypesFileName));

        for (var i = 0; i < contest.Votes.Count; i++)
        {
            contest.Votes[i].Vote.PoliticalBusinessNumber = $"Vote {i + 1}";
            contest.Votes[i].Vote.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau;
        }

        var request = new ImportPoliticalBusinessesRequest
        {
            ContestId = ContestMockedData.IdGossau,
            Votes = { contest.Votes },
        };
        await CantonAdminClient.ImportPoliticalBusinessesAsync(request);
    }

    [Fact]
    public async Task TestDomainOfInfluenceNotInParentShouldThrow()
    {
        var request = await CreateValidRequest(DomainOfInfluenceMockedData.IdGenf);
        await AssertStatus(
            async () => await CantonAdminClient.ImportPoliticalBusinessesAsync(request),
            StatusCode.InvalidArgument,
            "Invalid domain of influence(s), some ids are not children of the parent node");
    }

    [Fact]
    public async Task TestInvalidProportionalElectionMandateAlgorithmByCantonSettingsShouldThrow()
    {
        var request = await CreateValidRequest();
        request.ProportionalElections[0].Election.MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum;
        await AssertStatus(
            async () => await CantonAdminClient.ImportPoliticalBusinessesAsync(request),
            StatusCode.InvalidArgument,
            "mandate algorithm");
    }

    [Fact]
    public async Task TestInvalidVoteResultAlgorithmByDomainOfInfluenceTypeShouldThrow()
    {
        var request = await CreateValidRequest();
        request.Votes[0].Vote.ResultAlgorithm = SharedProto.VoteResultAlgorithm.PopularAndCountingCircleMajority;
        await AssertStatus(
            async () => await CantonAdminClient.ImportPoliticalBusinessesAsync(request),
            StatusCode.InvalidArgument,
            "Political domain of influence does not allow vote result algorithm");
    }

    [Fact]
    public async Task TestWithDuplicatedMajorityElectionPoliticalBusinessNumberShouldThrow()
    {
        var request = await CreateValidRequest();
        request.MajorityElections[0].Election.PoliticalBusinessNumber = request.SecondaryMajorityElections[0].Election.PoliticalBusinessNumber;
        await AssertStatus(
            async () => await CantonAdminClient.ImportPoliticalBusinessesAsync(request),
            StatusCode.AlreadyExists,
            $"DuplicatedPoliticalBusinessNumberException: The political business number {request.MajorityElections[0].Election.PoliticalBusinessNumber} is already taken");
    }

    [Fact]
    public async Task TestWithExistingMajorityElectionPoliticalBusinessNumberShouldThrow()
    {
        var request = await CreateValidRequest();
        request.MajorityElections[0].Election.PoliticalBusinessNumber = "324";
        await AssertStatus(
            async () => await CantonAdminClient.ImportPoliticalBusinessesAsync(request),
            StatusCode.AlreadyExists,
            $"DuplicatedPoliticalBusinessNumberException: The political business number {request.MajorityElections[0].Election.PoliticalBusinessNumber} is already taken");
    }

    [Fact]
    public async Task TestWithInvalidMajorityElectionReportDomainOfInfluenceLevelShouldThrow()
    {
        var request = await CreateValidRequest();
        request.MajorityElections[0].Election.ReportDomainOfInfluenceLevel = 9;
        await AssertStatus(
            async () => await CantonAdminClient.ImportPoliticalBusinessesAsync(request),
            StatusCode.InvalidArgument,
            $"Report domainOfInfluence level {request.MajorityElections[0].Election.ReportDomainOfInfluenceLevel} is invalid.");
    }

    [Fact]
    public async Task TestWithDuplicatedProportionalElectionPoliticalBusinessNumberShouldThrow()
    {
        var request = await CreateValidRequest();
        var propElection = request.ProportionalElections[0].Clone();
        request.ProportionalElections.Add(propElection);

        await AssertStatus(
            async () => await CantonAdminClient.ImportPoliticalBusinessesAsync(request),
            StatusCode.AlreadyExists,
            $"DuplicatedPoliticalBusinessNumberException: The political business number {propElection.Election.PoliticalBusinessNumber} is already taken");
    }

    [Fact]
    public async Task TestWithExistingProportionalElectionPoliticalBusinessNumberShouldThrow()
    {
        var request = await CreateValidRequest();
        request.ProportionalElections[0].Election.PoliticalBusinessNumber = "324";
        await AssertStatus(
            async () => await CantonAdminClient.ImportPoliticalBusinessesAsync(request),
            StatusCode.AlreadyExists,
            $"DuplicatedPoliticalBusinessNumberException: The political business number {request.ProportionalElections[0].Election.PoliticalBusinessNumber} is already taken");
    }

    [Fact]
    public async Task TestWithDuplicatedVotePoliticalBusinessNumberShouldThrow()
    {
        var request = await CreateValidRequest();
        var voteImport = request.Votes[0].Clone();
        request.Votes.Add(voteImport);

        await AssertStatus(
            async () => await CantonAdminClient.ImportPoliticalBusinessesAsync(request),
            StatusCode.AlreadyExists,
            $"DuplicatedPoliticalBusinessNumberException: The political business number {voteImport.Vote.PoliticalBusinessNumber} is already taken");
    }

    [Fact]
    public async Task TestWithTopLevelDomainOfInfluenceLevelShouldThrow()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            x => x.Id == DomainOfInfluenceMockedData.GuidGossau,
            x => x.VirtualTopLevel = true);

        var request = await CreateValidRequest();
        await AssertStatus(
            async () => await CantonAdminClient.ImportPoliticalBusinessesAsync(request),
            StatusCode.InvalidArgument,
            "A virtual top level domain of influence is not allowed for political businesses.");
    }

    [Fact]
    public async Task TestWithExistingVotePoliticalBusinessNumberShouldThrow()
    {
        var request = await CreateValidRequest();
        request.Votes[0].Vote.PoliticalBusinessNumber = "324";
        await AssertStatus(
            async () => await CantonAdminClient.ImportPoliticalBusinessesAsync(request),
            StatusCode.AlreadyExists,
            $"DuplicatedPoliticalBusinessNumberException: The political business number {request.Votes[0].Vote.PoliticalBusinessNumber} is already taken");
    }

    [Fact]
    public async Task TestWithInvalidVoteReportDomainOfInfluenceLevelShouldThrow()
    {
        var request = await CreateValidRequest();
        request.Votes[0].Vote.ReportDomainOfInfluenceLevel = 9;
        await AssertStatus(
            async () => await CantonAdminClient.ImportPoliticalBusinessesAsync(request),
            StatusCode.InvalidArgument,
            $"Report domainOfInfluence level {request.Votes[0].Vote.ReportDomainOfInfluenceLevel} is invalid.");
    }

    [Fact]
    public async Task TestWithDuplicatedCandidateIdShouldThrow()
    {
        var request = await CreateValidRequest();
        request.ProportionalElections[0].Lists[0].Candidates[0].Candidate.Id = request.ProportionalElections[0].Lists[0].Candidates[1].Candidate.Id;
        await AssertStatus(
            async () => await CantonAdminClient.ImportPoliticalBusinessesAsync(request),
            StatusCode.InvalidArgument,
            "This id is not unique");
    }

    [Fact]
    public async Task TestShouldThrowIfEVotingPoliticalBusinessAndPastEVotingApprovalDate()
    {
        var request = await CreateValidRequest(contestId: ContestMockedData.IdStGallenEvoting);

        await ModifyDbEntities<Data.Models.Contest>(
            c => c.Id == Guid.Parse(request.ContestId),
            c => c.EVotingApproved = true);

        await AssertStatus(
            async () => await CantonAdminClient.ImportPoliticalBusinessesAsync(request),
            StatusCode.InvalidArgument,
            "Cannot create a new e-voting political business when the contest e-voting approval has been set.");
    }

    [Fact]
    public override async Task AuthorizedOtherTenantAsElectionAdminShouldThrow()
    {
        var channel = CreateGrpcChannel(
            tenant: DomainOfInfluenceMockedData.Bund.SecureConnectId,
            roles: Roles.ElectionAdmin);

        await AssertStatus(
            async () => await AuthorizationTestCall(channel),
            StatusCode.NotFound);
    }

    [Fact]
    public override async Task AuthorizedOtherTenantAndDifferentCantonAsCantonAdminShouldThrow()
    {
        await ModifyDbEntities<Data.Models.DomainOfInfluence>(
            doi => true,
            doi => doi.Canton = Data.Models.DomainOfInfluenceCanton.Gr);

        var channel = CreateGrpcChannel(
            tenant: DomainOfInfluenceMockedData.Bund.SecureConnectId,
            roles: Roles.CantonAdmin);

        await AssertStatus(
            async () => await AuthorizationTestCall(channel),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ImportService.ImportServiceClient(channel)
            .ImportPoliticalBusinessesAsync(await CreateValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    private async Task<ImportPoliticalBusinessesRequest> CreateValidRequest(
        string doiId = DomainOfInfluenceMockedData.IdGossau,
        string contestId = ContestMockedData.IdGossau,
        bool v5 = false)
    {
        var contest1 = await LoadContestImport(SharedProto.ImportType.Ech157, EchTestFiles.GetTestFilePath(v5 ? EchTestFiles.Ech0157V5FileName : EchTestFiles.Ech0157FileName));
        var contest2 = await LoadContestImport(SharedProto.ImportType.Ech159, EchTestFiles.GetTestFilePath(v5 ? EchTestFiles.Ech0159V5FileName : EchTestFiles.Ech0159FileName));

        contest1.MajorityElections.AddRange(contest2.MajorityElections);
        contest1.SecondaryMajorityElections.AddRange(contest2.SecondaryMajorityElections);
        contest1.ProportionalElections.AddRange(contest2.ProportionalElections);
        contest1.Votes.AddRange(contest2.Votes);

        foreach (var (majorityElection, index) in contest1.MajorityElections.Select((x, i) => (x, i)))
        {
            majorityElection.Election.PoliticalBusinessNumber = $"11newME{index}";
            majorityElection.Election.DomainOfInfluenceId = doiId;
        }

        foreach (var (secondaryMajorityElection, index) in contest1.SecondaryMajorityElections.Select((x, i) => (x, i)))
        {
            secondaryMajorityElection.Election.PoliticalBusinessNumber = $"12newSME{index}";
        }

        foreach (var (proportionalElection, index) in contest1.ProportionalElections.Select((x, i) => (x, i)))
        {
            proportionalElection.Election.PoliticalBusinessNumber = $"11newPE{index}";
            proportionalElection.Election.DomainOfInfluenceId = doiId;
        }

        foreach (var (vote, index) in contest1.Votes.Select((x, i) => (x, i)))
        {
            vote.Vote.PoliticalBusinessNumber = $"11newV{index}";
            vote.Vote.DomainOfInfluenceId = doiId;
        }

        return new ImportPoliticalBusinessesRequest
        {
            ContestId = contestId,
            MajorityElections = { contest1.MajorityElections },
            SecondaryMajorityElections = { contest1.SecondaryMajorityElections },
            ProportionalElections = { contest1.ProportionalElections },
            Votes = { contest1.Votes },
        };
    }
}
