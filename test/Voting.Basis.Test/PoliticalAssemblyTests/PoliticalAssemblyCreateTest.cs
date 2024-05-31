// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
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
using Voting.Basis.Core.Auth;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.PoliticalAssemblyTests;

public class PoliticalAssemblyCreateTest : BaseGrpcTest<PoliticalAssemblyService.PoliticalAssemblyServiceClient>
{
    public PoliticalAssemblyCreateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await PoliticalAssemblyMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task Test()
    {
        var response = await AdminClient.CreateAsync(NewValidRequest());

        var eventData = EventPublisherMock.GetSinglePublishedEvent<PoliticalAssemblyCreated>();

        eventData.PoliticalAssembly.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.PoliticalAssembly.Id);
    }

    [Fact]
    public async Task TestAggregate()
    {
        await RunOnDb(async db =>
        {
            var set = await db.PoliticalAssemblies.ToListAsync();
            db.PoliticalAssemblies.RemoveRange(set);
            await db.SaveChangesAsync();
        });

        var politicalAssemblyId1 = Guid.Parse("98afe54b-2c64-4acf-8849-77052138dc4d");
        var politicalAssemblyId2 = Guid.Parse("00fde268-ea53-4d28-8ba8-ab2fd5f7d643");

        var politicalAssemblyEv1 = new PoliticalAssemblyCreated
        {
            PoliticalAssembly = new PoliticalAssemblyEventData
            {
                Id = politicalAssemblyId1.ToString(),
                Date = new DateTime(2020, 8, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                Description = { LanguageUtil.MockAllLanguages("test") },
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
            },
        };

        var politicalAssemblyEv2 = new PoliticalAssemblyCreated
        {
            PoliticalAssembly = new PoliticalAssemblyEventData
            {
                Id = politicalAssemblyId2.ToString(),
                Date = new DateTime(2020, 8, 24, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                Description = { LanguageUtil.MockAllLanguages("test") },
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
            },
        };

        await TestEventPublisher.Publish(
            politicalAssemblyEv1,
            politicalAssemblyEv2);

        var politicalAssemblies = await AdminClient.ListAsync(new ListPoliticalAssemblyRequest());
        politicalAssemblies.PoliticalAssemblies_.Should().HaveCount(2);
        politicalAssemblies.MatchSnapshot();
    }

    [Fact]
    public async Task NoDescriptionShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o => o.Description.Clear())),
            StatusCode.InvalidArgument,
            "Description");
    }

    [Fact]
    public async Task DateBeforeNowShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o => o.Date = MockedClock.GetTimestampDate(-10))),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task OtherTenantDomainOfInfluenceIdShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.CreateAsync(
                NewValidRequest(o => o.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdUzwil)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task DomainOfInfluenceNotResponsibleForVotingCardsShouldThrow()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            c => c.Id == DomainOfInfluenceMockedData.GuidGossau,
            c => c.ResponsibleForVotingCards = false);

        await AssertStatus(
            async () => await ElectionAdminClient.CreateAsync(NewValidRequest()),
            StatusCode.InvalidArgument);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new PoliticalAssemblyService.PoliticalAssemblyServiceClient(channel)
            .CreateAsync(NewValidRequest());

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
    }

    private CreatePoliticalAssemblyRequest NewValidRequest(
        Action<CreatePoliticalAssemblyRequest>? customizer = null)
    {
        var request = new CreatePoliticalAssemblyRequest
        {
            Date = new DateTime(2020, 12, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            Description = { LanguageUtil.MockAllLanguages("test") },
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
        };
        customizer?.Invoke(request);
        return request;
    }
}
