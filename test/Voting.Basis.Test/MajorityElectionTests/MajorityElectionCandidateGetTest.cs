// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Basis.Core.Auth;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.MajorityElectionTests;

public class MajorityElectionCandidateGetTest : BaseGrpcTest<MajorityElectionService.MajorityElectionServiceClient>
{
    private const string IdNotFound = "eae2cfaf-c787-48b9-a108-c975b0addddd";

    public MajorityElectionCandidateGetTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await MajorityElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestAsAdminShouldReturnOk()
    {
        var response = await AdminClient.GetCandidateAsync(new GetMajorityElectionCandidateRequest
        {
            Id = MajorityElectionMockedData.CandidateIdGossauMajorityElectionInContestGossau,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturnOk()
    {
        var response = await ElectionAdminClient.GetCandidateAsync(new GetMajorityElectionCandidateRequest
        {
            Id = MajorityElectionMockedData.CandidateIdGossauMajorityElectionInContestGossau,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsAdminParentDomainOfInfluenceShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.GetCandidateAsync(new GetMajorityElectionCandidateRequest
            {
                Id = MajorityElectionMockedData.CandidateId1BundMajorityElectionInContestStGallen,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestAsElectionAdminParentDomainOfInfluenceShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.GetCandidateAsync(new GetMajorityElectionCandidateRequest
            {
                Id = MajorityElectionMockedData.CandidateId1BundMajorityElectionInContestStGallen,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestAsAdminChildDomainOfInfluenceShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.GetCandidateAsync(new GetMajorityElectionCandidateRequest
            {
                Id = MajorityElectionMockedData.CandidateIdUzwilMajorityElectionInContestStGallen,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestAsElectionAdminChildDomainOfInfluenceShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.GetCandidateAsync(new GetMajorityElectionCandidateRequest
            {
                Id = MajorityElectionMockedData.CandidateIdUzwilMajorityElectionInContestStGallen,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestForeignDoiShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.GetCandidateAsync(new GetMajorityElectionCandidateRequest
            {
                Id = MajorityElectionMockedData.CandidateIdKircheMajorityElectionInContestKirche,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestNotFound()
    {
        await AssertStatus(
            async () => await AdminClient.GetCandidateAsync(new GetMajorityElectionCandidateRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .GetCandidateAsync(new GetMajorityElectionCandidateRequest
            {
                Id = MajorityElectionMockedData.CandidateIdGossauMajorityElectionInContestGossau,
            });
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }
}
