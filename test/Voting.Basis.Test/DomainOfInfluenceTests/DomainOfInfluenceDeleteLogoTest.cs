// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Snapper;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Extensions;
using Voting.Basis.Core.ObjectStorage;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Store;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.ObjectStorage.Testing.Mocks;
using Xunit;

namespace Voting.Basis.Test.DomainOfInfluenceTests;

public class DomainOfInfluenceDeleteLogoTest : BaseGrpcTest<DomainOfInfluenceService.DomainOfInfluenceServiceClient>
{
    public DomainOfInfluenceDeleteLogoTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await DomainOfInfluenceMockedData.Seed(RunScoped);

        var doiIdSg = DomainOfInfluenceMockedData.GuidStGallen;
        string? logoRef = string.Empty;

        await RunScoped(async (IServiceProvider sp) =>
        {
            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues(string.Empty, "test", "test", Enumerable.Empty<string>());

            var repo = sp.GetRequiredService<AggregateRepositoryMock>();

            var aggregate = await repo.GetById<DomainOfInfluenceAggregate>(doiIdSg);
            aggregate.UpdateLogo();
            await repo.Save(aggregate);

            logoRef = aggregate.LogoRef;
        });

        await RunOnDb(async db =>
        {
            var doi = await db.DomainOfInfluences.FirstAsync(x => x.Id == doiIdSg);
            doi.LogoRef = logoRef;
            db.Update(doi);
            await db.SaveChangesAsync();
        });

        await using var ms = new MemoryStream(Encoding.UTF8.GetBytes("hello world"));
        await GetService<DomainOfInfluenceLogoStorage>()
            .Store(logoRef, ms);
    }

    [Fact]
    public async Task ShouldDeleteLogo()
    {
        var bucket = "voting";
        var objectName = "domain-of-influence-logos/v1/" + DomainOfInfluenceMockedData.IdStGallen;

        GetService<ObjectStorageClientMock>()
            .Exists(bucket, objectName)
            .Should()
            .BeTrue();

        await AdminClient.DeleteLogoAsync(new DeleteDomainOfInfluenceLogoRequest
        { DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen });

        GetService<ObjectStorageClientMock>()
            .Exists(bucket, objectName)
            .Should()
            .BeFalse();

        EventPublisherMock
            .GetSinglePublishedEvent<DomainOfInfluenceLogoDeleted>()
            .ShouldMatchSnapshot();
    }

    [Fact]
    public async Task ShouldDeleteLogoAsElectionAdmin()
    {
        await ElectionAdminClient.DeleteLogoAsync(new DeleteDomainOfInfluenceLogoRequest
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
        });

        EventPublisherMock
            .GetSinglePublishedEvent<DomainOfInfluenceLogoDeleted>()
            .DomainOfInfluenceId
            .Should()
            .Be(DomainOfInfluenceMockedData.IdStGallen);
    }

    [Fact]
    public Task ShouldThrowOtherTenant()
    {
        var req = new DeleteDomainOfInfluenceLogoRequest
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdBund,
        };
        return AssertStatus(
            async () => await ElectionAdminClient.DeleteLogoAsync(req),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public Task ShouldThrowNoLogo()
    {
        var req = new DeleteDomainOfInfluenceLogoRequest
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdBund,
        };
        return AssertStatus(
            async () => await AdminClient.DeleteLogoAsync(req),
            StatusCode.InvalidArgument,
            "cannot delete logo if no logo reference is set");
    }

    [Fact]
    public async Task ProcessorShouldWork()
    {
        var logoRef = "v1/" + DomainOfInfluenceMockedData.IdStGallen;
        await TestEventPublisher.Publish(new DomainOfInfluenceLogoDeleted
        {
            EventInfo = new EventInfo
            {
                Timestamp = new Timestamp
                {
                    Seconds = 1594980476,
                },
                Tenant = SecureConnectTestDefaults.MockedTenantDefault.ToEventInfoTenant(),
                User = SecureConnectTestDefaults.MockedUserDefault.ToEventInfoUser(),
            },
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
            LogoRef = logoRef,
        });

        var doi = await RunOnDb(db =>
            db.DomainOfInfluences.FirstAsync(x => x.Id == DomainOfInfluenceMockedData.GuidStGallen));
        doi.HasLogo.Should().BeFalse();
        doi.LogoRef.Should().BeNull();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new DomainOfInfluenceService.DomainOfInfluenceServiceClient(channel)
            .DeleteLogoAsync(
                new DeleteDomainOfInfluenceLogoRequest { DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen });
    }
}
