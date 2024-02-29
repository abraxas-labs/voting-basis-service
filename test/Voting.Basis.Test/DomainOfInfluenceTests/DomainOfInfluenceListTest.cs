// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.DomainOfInfluenceTests;

public class DomainOfInfluenceListTest : BaseGrpcTest<DomainOfInfluenceService.DomainOfInfluenceServiceClient>
{
    public DomainOfInfluenceListTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestAsAdminShouldReturnAllWithMatchingCountingCircleId()
    {
        var response = await AdminClient.ListAsync(new ListDomainOfInfluenceRequest
        {
            CountingCircleId = CountingCircleMockedData.IdStGallen,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsAdminShouldReturnAllWithMatchingSecureConnectId()
    {
        var response = await AdminClient.ListAsync(new ListDomainOfInfluenceRequest
        {
            SecureConnectId = DomainOfInfluenceMockedData.StGallen.SecureConnectId,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturnAllWithMatchingCountingCircleId()
    {
        var response = await ElectionAdminClient.ListAsync(new ListDomainOfInfluenceRequest
        {
            CountingCircleId = CountingCircleMockedData.IdStGallen,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturnAllWithMatchingSecureConnectId()
    {
        var response = await ElectionAdminClient.ListAsync(new ListDomainOfInfluenceRequest
        {
            SecureConnectId = DomainOfInfluenceMockedData.StGallen.SecureConnectId,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminForeignSecureConnectIdShouldThrow()
    {
        await AssertStatus(
            async () =>
            await ElectionAdminClient.ListAsync(new ListDomainOfInfluenceRequest
            {
                SecureConnectId = DomainOfInfluenceMockedData.Uzwil.SecureConnectId,
            }),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestAsAdminNotExistingShouldReturnEmpty()
    {
        await RunOnDb(async db =>
        {
            db.DomainOfInfluences.RemoveRange(db.DomainOfInfluences);
            await db.SaveChangesAsync();
        });
        var response = await AdminClient.ListAsync(new ListDomainOfInfluenceRequest
        {
            CountingCircleId = CountingCircleMockedData.IdNotExisting,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsAdminEmptyShouldReturnEmpty()
    {
        await RunOnDb(async db =>
        {
            db.DomainOfInfluences.RemoveRange(db.DomainOfInfluences);
            await db.SaveChangesAsync();
        });
        var response = await AdminClient.ListAsync(new ListDomainOfInfluenceRequest
        {
            CountingCircleId = CountingCircleMockedData.IdStGallen,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsAdminAllAccessibleForPoliticalBusiness()
    {
        var response = await AdminClient.ListAsync(new ListDomainOfInfluenceRequest
        {
            ContestDomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminAllAccessibleForPoliticalBusiness()
    {
        var response = await ElectionAdminClient.ListAsync(new ListDomainOfInfluenceRequest
        {
            ContestDomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestInvalidGuid()
        => await AssertStatus(
            async () => await AdminClient.ListAsync(new ListDomainOfInfluenceRequest
            {
                CountingCircleId = CountingCircleMockedData.IdInvalid,
            }),
            StatusCode.InvalidArgument);

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new DomainOfInfluenceService.DomainOfInfluenceServiceClient(channel)
            .ListAsync(new ListDomainOfInfluenceRequest());

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
