// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Basis.Core.Auth;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.PoliticalAssemblyTests;

public class PoliticalAssemblyUpdateTest : BaseGrpcTest<PoliticalAssemblyService.PoliticalAssemblyServiceClient>
{
    public PoliticalAssemblyUpdateTest(TestApplicationFactory factory)
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
        var req = NewValidRequest();
        await CantonAdminClient.UpdateAsync(req);
        var eventData = EventPublisherMock.GetSinglePublishedEvent<PoliticalAssemblyUpdated>();
        eventData.MatchSnapshot("event", d => d.PoliticalAssembly.Id);
    }

    [Fact]
    public async Task TestAggregate()
    {
        var ev = new PoliticalAssemblyUpdated
        {
            PoliticalAssembly = new PoliticalAssemblyEventData
            {
                Id = PoliticalAssemblyMockedData.IdGossau,
                Date = new DateTime(2020, 8, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                Description = { LanguageUtil.MockAllLanguages("test") },
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
            },
        };
        await TestEventPublisher.Publish(ev);
        var politicalAssembly = await CantonAdminClient.GetAsync(new GetPoliticalAssemblyRequest { Id = PoliticalAssemblyMockedData.IdGossau });
        politicalAssembly.MatchSnapshot();
    }

    [Fact]
    public async Task NoDescriptionShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateAsync(NewValidRequest(o => o.Description.Clear())),
            StatusCode.InvalidArgument,
            "Description");
    }

    [Fact]
    public async Task EndOfTestingPhaseBeforeNowShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateAsync(NewValidRequest(o =>
            {
                o.Date = MockedClock.GetTimestampDate(-10);
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task OtherTenantPoliticalAssemblyShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(
                NewValidRequest(o => o.Id = PoliticalAssemblyMockedData.IdKirche)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task DomainOfInfluenceNotResponsibleForVotingCardsShouldThrow()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            c => c.Id == DomainOfInfluenceMockedData.GuidGossau,
            c => c.ResponsibleForVotingCards = false);

        await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequest()),
            StatusCode.InvalidArgument);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new PoliticalAssemblyService.PoliticalAssemblyServiceClient(channel)
            .UpdateAsync(NewValidRequest());

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
    }

    private UpdatePoliticalAssemblyRequest NewValidRequest(
        Action<UpdatePoliticalAssemblyRequest>? customizer = null)
    {
        var request = new UpdatePoliticalAssemblyRequest
        {
            Id = PoliticalAssemblyMockedData.IdGossau,
            Date = new DateTime(2020, 8, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            Description = { LanguageUtil.MockAllLanguages("test") },
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
        };
        customizer?.Invoke(request);
        return request;
    }
}
