// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.ObjectStorage;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Store;
using Xunit;

namespace Voting.Basis.Test.DomainOfInfluenceTests;

public class DomainOfInfluenceGetLogoTest : BaseGrpcTest<DomainOfInfluenceService.DomainOfInfluenceServiceClient>
{
    public DomainOfInfluenceGetLogoTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await DomainOfInfluenceMockedData.Seed(RunScoped);

        var doiIdSg = DomainOfInfluenceMockedData.GuidStGallen;
        await RunOnDb(async db =>
        {
            var doi = await db.DomainOfInfluences.FirstAsync(x => x.Id == doiIdSg);
            doi.LogoRef = "sg-logo-ref.png";
            db.Update(doi);
            await db.SaveChangesAsync();
        });

        await RunScoped(async (IServiceProvider sp) =>
        {
            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues(string.Empty, "test", "test", Enumerable.Empty<string>());

            var repo = sp.GetRequiredService<AggregateRepositoryMock>();

            var aggregate = await repo.GetById<DomainOfInfluenceAggregate>(doiIdSg);
            aggregate.UpdateLogo();
            await repo.Save(aggregate);
        });

        await using var ms = new MemoryStream(Encoding.UTF8.GetBytes("hello world"));
        await GetService<DomainOfInfluenceLogoStorage>()
            .Store("sg-logo-ref.png", ms);
    }

    [Fact]
    public async Task ShouldReturnUrlAsAdmin()
    {
        var resp = await AdminClient.GetLogoAsync(new GetDomainOfInfluenceLogoRequest
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
        });
        resp.DomainOfInfluenceId.Should().Be(DomainOfInfluenceMockedData.IdStGallen);
        resp.LogoUrl.Should().Be("http://localhost:9000/voting/domain-of-influence-logos/sg-logo-ref.png?ttl=60");
    }

    [Fact]
    public async Task ShouldReturnUrlAsElectionAdmin()
    {
        var resp = await ElectionAdminClient.GetLogoAsync(new GetDomainOfInfluenceLogoRequest
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
        });
        resp.DomainOfInfluenceId.Should().Be(DomainOfInfluenceMockedData.IdStGallen);
        resp.LogoUrl.Should().Be("http://localhost:9000/voting/domain-of-influence-logos/sg-logo-ref.png?ttl=60");
    }

    [Fact]
    public async Task ShouldReturnUrlAsElectionAdminWithOtherTenant()
    {
        var resp = await ElectionAdminUzwilClient.GetLogoAsync(new GetDomainOfInfluenceLogoRequest
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
        });
        resp.DomainOfInfluenceId.Should().Be(DomainOfInfluenceMockedData.IdStGallen);
        resp.LogoUrl.Should().Be("http://localhost:9000/voting/domain-of-influence-logos/sg-logo-ref.png?ttl=60");
    }

    [Fact]
    public Task ShouldThrowWithoutReadAccess()
    {
        return AssertStatus(
            async () => await ElectionAdminClient.GetLogoAsync(new GetDomainOfInfluenceLogoRequest
            {
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdBund,
            }),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new DomainOfInfluenceService.DomainOfInfluenceServiceClient(channel)
            .GetLogoAsync(
                new GetDomainOfInfluenceLogoRequest { DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen });
    }
}
