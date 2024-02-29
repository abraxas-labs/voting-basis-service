// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.DomainOfInfluenceTests;

public class DomainOfInfluenceDeleteTest : BaseGrpcTest<DomainOfInfluenceService.DomainOfInfluenceServiceClient>
{
    public DomainOfInfluenceDeleteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestInvalidGuid()
        => await AssertStatus(
            async () => await AdminClient.DeleteAsync(new DeleteDomainOfInfluenceRequest
            {
                Id = DomainOfInfluenceMockedData.IdInvalid,
            }),
            StatusCode.InvalidArgument);

    [Fact]
    public async Task TestNotFound()
        => await AssertStatus(
            async () => await AdminClient.DeleteAsync(new DeleteDomainOfInfluenceRequest
            {
                Id = DomainOfInfluenceMockedData.IdNotExisting,
            }),
            StatusCode.NotFound);

    [Fact]
    public async Task TestShouldPublishDeletedEvent()
    {
        await AdminClient.DeleteAsync(new DeleteDomainOfInfluenceRequest
        {
            Id = DomainOfInfluenceMockedData.IdUzwil,
        });
        var eventData = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceDeleted>();

        eventData.DomainOfInfluenceId.Should().Be(DomainOfInfluenceMockedData.IdUzwil);
        eventData.MatchSnapshot();
    }

    [Fact]
    public async Task TestAggregateShouldRemoveFromDatabaseInclInheritedCountingCirclesOnParents()
    {
        await TestEventPublisher.Publish(new DomainOfInfluenceDeleted
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
            EventInfo = GetMockedEventInfo(),
        });
        (await RunOnDb(db => db.DomainOfInfluences
                .FirstOrDefaultAsync(di => di.Id == DomainOfInfluenceMockedData.GuidGossau)))
            .Should()
            .BeNull();
        (await RunOnDb(db => db.DomainOfInfluenceCountingCircles
                .FirstOrDefaultAsync(di => di.CountingCircleId == Guid.Parse(CountingCircleMockedData.IdGossau))))
            .Should()
            .BeNull();
        (await RunOnDb(db => db.ExportConfigurations
                .FirstOrDefaultAsync(di => di.DomainOfInfluenceId == DomainOfInfluenceMockedData.GuidGossau)))
            .Should()
            .BeNull();
    }

    [Fact]
    public async Task TestIndirectCascadeDeleteShouldWork()
    {
        // invalid cascade delete: doi delete doi partys but also proportional elections.
        // proportional election deletes candidates which depends on doi party.
        await RunOnDb(async db =>
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
        });

        await ProportionalElectionMockedData.Seed(RunScoped);

        // should throw no exception
        await TestEventPublisher.Publish(new DomainOfInfluenceDeleted
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
            EventInfo = GetMockedEventInfo(),
        });
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new DomainOfInfluenceService.DomainOfInfluenceServiceClient(channel)
            .DeleteAsync(new DeleteDomainOfInfluenceRequest { Id = DomainOfInfluenceMockedData.IdBund });

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return Roles.ElectionAdmin;
    }
}
