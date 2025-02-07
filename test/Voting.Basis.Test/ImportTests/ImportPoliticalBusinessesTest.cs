// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
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
