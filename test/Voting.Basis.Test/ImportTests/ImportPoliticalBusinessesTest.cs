// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Basis.Test.MockedData;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.ImportTests;

public class ImportPoliticalBusinessesTest : BaseImportTest
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
        await AdminClient.ImportPoliticalBusinessesAsync(request);
    }

    [Fact]
    public async Task TestDomainOfInfluenceNotInParentShouldThrow()
    {
        var request = await CreateValidRequest(DomainOfInfluenceMockedData.IdGenf);
        await AssertStatus(
            async () => await AdminClient.ImportPoliticalBusinessesAsync(request),
            StatusCode.InvalidArgument,
            "Invalid domain of influence(s), some ids are not children of the parent node");
    }

    [Fact]
    public async Task TestDomainOfInfluenceOtherTenantShouldThrow()
    {
        var request = await CreateValidRequest(DomainOfInfluenceMockedData.IdUzwil, ContestMockedData.IdBundContest);
        await AssertStatus(
            async () => await AdminClient.ImportPoliticalBusinessesAsync(request),
            StatusCode.InvalidArgument,
            $"Domain of influence with id {DomainOfInfluenceMockedData.IdUzwil} does not belong to this tenant");
    }

    [Fact]
    public async Task TestInvalidProportionalElectionMandateAlgorithmByCantonSettingsShouldThrow()
    {
        var request = await CreateValidRequest();
        request.ProportionalElections[0].Election.MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.DoppelterPukelsheim5Quorum;
        await AssertStatus(
            async () => await AdminClient.ImportPoliticalBusinessesAsync(request),
            StatusCode.InvalidArgument,
            "mandate algorithm");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ImportService.ImportServiceClient(channel)
            .ImportPoliticalBusinessesAsync(await CreateValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    private async Task<ImportPoliticalBusinessesRequest> CreateValidRequest(
        string doiId = DomainOfInfluenceMockedData.IdGossau,
        string contestId = ContestMockedData.IdGossau)
    {
        var contest1 = await AdminClient.ResolveImportFileAsync(new ResolveImportFileRequest
        {
            ImportType = SharedProto.ImportType.Ech157,
            FileContent = await GetTestEch0157File(),
        });
        var contest2 = await AdminClient.ResolveImportFileAsync(new ResolveImportFileRequest
        {
            ImportType = SharedProto.ImportType.Ech159,
            FileContent = await GetTestEch0159File(),
        });

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
