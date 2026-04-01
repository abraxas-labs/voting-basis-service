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
    }

    [Fact]
    public async Task TestShouldWork()
    {
        var request = await CreateValidRequest();
        await CantonAdminClient.ImportPoliticalBusinessesAsync(request);
    }

    [Fact]
    public async Task TestShouldNotEnableEVotingApproval()
    {
        var request = await CreateValidRequest(contestId: ContestMockedData.IdStGallenEvoting);
        request.Votes[0].Vote.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdUzwil;
        request.MajorityElections[0].Election.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdUzwil;

        await CantonAdminClient.ImportPoliticalBusinessesAsync(request);

        var peCreatedEvents = EventPublisherMock.GetPublishedEvents<ProportionalElectionCreated>();
        var meCreatedEvents = EventPublisherMock.GetPublishedEvents<MajorityElectionCreated>();
        var voteCreatedEvents = EventPublisherMock.GetPublishedEvents<VoteCreated>();

        // political business e-voting is currently disabled.
        voteCreatedEvents.First().Vote.EVotingApproved.Should().BeNull();
        meCreatedEvents.First().MajorityElection.EVotingApproved.Should().BeNull();
        voteCreatedEvents.Any(v => v.Vote.EVotingApproved == false).Should().BeFalse();
        peCreatedEvents.Any(pe => pe.ProportionalElection.EVotingApproved == false).Should().BeFalse();
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
        string contestId = ContestMockedData.IdGossau)
    {
        var contest1 = await LoadContestImport(SharedProto.ImportType.Ech157, EchTestFiles.GetTestFilePath(EchTestFiles.Ech0157FileName));
        var contest2 = await LoadContestImport(SharedProto.ImportType.Ech159, EchTestFiles.GetTestFilePath(EchTestFiles.Ech0159FileName));

        contest1.MajorityElections.AddRange(contest2.MajorityElections);
        contest1.ProportionalElections.AddRange(contest2.ProportionalElections);
        contest1.Votes.AddRange(contest2.Votes);

        foreach (var majorityElection in contest1.MajorityElections)
        {
            majorityElection.Election.PoliticalBusinessNumber = "11new";
            majorityElection.Election.DomainOfInfluenceId = doiId;
        }

        foreach (var proportionalElection in contest1.ProportionalElections)
        {
            proportionalElection.Election.PoliticalBusinessNumber = "11new";
            proportionalElection.Election.DomainOfInfluenceId = doiId;
        }

        foreach (var vote in contest1.Votes)
        {
            vote.Vote.PoliticalBusinessNumber = "11new";
            vote.Vote.DomainOfInfluenceId = doiId;
        }

        return new ImportPoliticalBusinessesRequest
        {
            ContestId = contestId,
            MajorityElections = { contest1.MajorityElections },
            ProportionalElections = { contest1.ProportionalElections },
            Votes = { contest1.Votes },
        };
    }
}
