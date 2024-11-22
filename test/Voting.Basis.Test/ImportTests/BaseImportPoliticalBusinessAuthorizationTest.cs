// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Grpc.Core;
using Voting.Basis.Core.Auth;
using Voting.Basis.Test.MockedData;
using Xunit;

namespace Voting.Basis.Test.ImportTests;

public abstract class BaseImportPoliticalBusinessAuthorizationTest : BaseImportTest
{
    protected BaseImportPoliticalBusinessAuthorizationTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public virtual async Task AuthorizedOtherTenantAsElectionAdminShouldThrow()
    {
        var channel = CreateGrpcChannel(
            tenant: DomainOfInfluenceMockedData.Bund.SecureConnectId,
            roles: Roles.ElectionAdmin);

        await AssertStatus(
            async () => await AuthorizationTestCall(channel),
            StatusCode.PermissionDenied,
            "Invalid domain of influence, does not belong to this tenant");
    }

    [Fact]
    public virtual async Task AuthorizedOtherTenantAndDifferentCantonAsCantonAdminShouldThrow()
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
            "Invalid domain of influence, does not belong to any owning canton");
    }

    [Fact]
    public virtual async Task AuthorizedOtherTenantAndSameCantonAsCantonAdminShouldWork()
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

    [Fact]
    public virtual async Task AuthorizedOtherTenantAsAdminShouldThrow()
    {
        var channel = CreateGrpcChannel(
            tenant: DomainOfInfluenceMockedData.Bund.SecureConnectId,
            roles: Roles.Admin);

        await AssertStatus(
            async () => await AuthorizationTestCall(channel),
            StatusCode.PermissionDenied,
            "Invalid domain of influence, does not belong to this tenant");
    }
}
