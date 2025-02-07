// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Grpc.Core;
using Voting.Basis.Core.Auth;
using Voting.Basis.Test.MockedData;
using Xunit;

namespace Voting.Basis.Test;

public abstract class PoliticalBusinessUnionAuthorizationGrpcBaseTest<TService> : BaseGrpcTest<TService>
    where TService : ClientBase<TService>
{
    protected PoliticalBusinessUnionAuthorizationGrpcBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task AuthorizedOtherTenantAsElectionAdminShouldThrow()
    {
        var channel = CreateGrpcChannel(
            tenant: DomainOfInfluenceMockedData.Bund.SecureConnectId,
            roles: Roles.ElectionAdmin);

        await AssertStatus(
            async () => await AuthorizationTestCall(channel),
            StatusCode.PermissionDenied,
            "Insufficient permissions for the political business union update");
    }

    [Fact]
    public async Task AuthorizedOtherTenantAndDifferentCantonAsCantonAdminShouldThrow()
    {
        await ModifyDbEntities<Data.Models.DomainOfInfluence>(
            doi => true,
            doi => doi.Canton = Data.Models.DomainOfInfluenceCanton.Gr);

        var channel = CreateGrpcChannel(
            tenant: DomainOfInfluenceMockedData.Bund.SecureConnectId,
            roles: Roles.CantonAdmin);

        await AssertStatus(
            async () => await AuthorizationTestCall(channel),
            StatusCode.PermissionDenied,
            "Insufficient permissions for the political business union update");
    }

    [Fact]
    public async Task AuthorizedOtherTenantAndSameCantonAsCantonAdminShouldWork()
    {
        await ModifyDbEntities<Data.Models.CantonSettings>(
            c => c.Canton == Data.Models.DomainOfInfluenceCanton.Sg,
            c => c.SecureConnectId = DomainOfInfluenceMockedData.Bund.SecureConnectId);

        var channel = CreateGrpcChannel(
            tenant: DomainOfInfluenceMockedData.Bund.SecureConnectId,
            roles: Roles.CantonAdmin);

        try
        {
            await AuthorizationTestCall(channel);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Call failed {ex}");
        }
    }
}
