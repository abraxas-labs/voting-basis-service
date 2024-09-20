// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Basis.Core.Auth;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.ProportionalElectionUnionTests;

public class ProportionalElectionUnionListTest : BaseGrpcTest<ProportionalElectionUnionService.ProportionalElectionUnionServiceClient>
{
    public ProportionalElectionUnionListTest(TestApplicationFactory factory)
       : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ProportionalElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestShouldReturnOk()
    {
        var response = await ElectionAdminClient.ListAsync(
            new ListProportionalElectionUnionsRequest
            {
                ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
            });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task DifferentTenantButAccessOnSameContestShouldReturnOk()
    {
        var response = await ElectionAdminClient.ListAsync(
            new ListProportionalElectionUnionsRequest
            {
                ProportionalElectionId = ProportionalElectionMockedData.IdUzwilProportionalElectionInContestBund,
            });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task NoAccessOnContestShouldThrow()
    {
        await RunOnDb(async db =>
        {
            db.ProportionalElectionUnionEntries.Add(new ProportionalElectionUnionEntry
            {
                Id = Guid.NewGuid(),
                ProportionalElectionUnionId = ProportionalElectionUnionMockedData.Kirche.Id,
                ProportionalElectionId = ProportionalElectionMockedData.KircheProportionalElectionInContestKirche.Id,
            });
            await db.SaveChangesAsync();
        });

        await AssertStatus(
            async () => await ElectionAdminClient.ListAsync(
                new ListProportionalElectionUnionsRequest
                {
                    ProportionalElectionId = ProportionalElectionMockedData.IdKircheProportionalElectionInContestKirche,
                }),
            StatusCode.PermissionDenied);
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionUnionService.ProportionalElectionUnionServiceClient(channel)
            .ListAsync(new ListProportionalElectionUnionsRequest
            {
                ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
            });
    }
}
