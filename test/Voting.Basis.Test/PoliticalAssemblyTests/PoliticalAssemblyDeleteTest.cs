// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.PoliticalAssemblyTests;

public class PoliticalAssemblyDeleteTest : BaseGrpcTest<PoliticalAssemblyService.PoliticalAssemblyServiceClient>
{
    private const string IdNotFound = "bfe2cfaf-c787-48b9-a108-c975b0addddd";
    private const string IdInvalid = "eae2xxxx";
    private string? _authTestPoliticalAssemblyId;

    public PoliticalAssemblyDeleteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await PoliticalAssemblyMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task InvalidGuidShouldThrow()
        => await AssertStatus(
            async () => await ElectionAdminClient.DeleteAsync(new DeletePoliticalAssemblyRequest
            {
                Id = IdInvalid,
            }),
            StatusCode.InvalidArgument);

    [Fact]
    public async Task TestNotFound()
        => await AssertStatus(
            async () => await ElectionAdminClient.DeleteAsync(new DeletePoliticalAssemblyRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.NotFound);

    [Fact]
    public async Task Test()
    {
        await ElectionAdminClient.DeleteAsync(new DeletePoliticalAssemblyRequest
        {
            Id = PoliticalAssemblyMockedData.IdGossau,
        });
        var eventData = EventPublisherMock.GetSinglePublishedEvent<PoliticalAssemblyDeleted>();

        eventData.PoliticalAssemblyId.Should().Be(PoliticalAssemblyMockedData.IdGossau);
        eventData.MatchSnapshot();
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = PoliticalAssemblyMockedData.IdGossau;
        await TestEventPublisher.Publish(new PoliticalAssemblyDeleted { PoliticalAssemblyId = id });

        var idGuid = Guid.Parse(id);
        (await RunOnDb(db => db.PoliticalAssemblies.CountAsync(c => c.Id == idGuid)))
            .Should().Be(0);
    }

    [Fact]
    public async Task TestForeignDomainOfInfluenceShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteAsync(new DeletePoliticalAssemblyRequest
            {
                Id = PoliticalAssemblyMockedData.IdKirche,
            }),
            StatusCode.InvalidArgument);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        if (_authTestPoliticalAssemblyId == null)
        {
            var response = await ElectionAdminClient.CreateAsync(new CreatePoliticalAssemblyRequest
            {
                Date = new DateTime(2020, 12, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                Description = { LanguageUtil.MockAllLanguages("test") },
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
            });

            _authTestPoliticalAssemblyId = response.Id;
        }

        await new PoliticalAssemblyService.PoliticalAssemblyServiceClient(channel)
            .DeleteAsync(new DeletePoliticalAssemblyRequest { Id = _authTestPoliticalAssemblyId });
        _authTestPoliticalAssemblyId = null;
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
    }
}
