// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Basis.Core.Auth;
using Voting.Basis.Test.MockedData;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.ContestTests;

public class ContestCheckAvailabilityTest : BaseGrpcTest<ContestService.ContestServiceClient>
{
    public ContestCheckAvailabilityTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestInvalidDomainOfInfluenceShouldThrow()
    {
        await AssertStatus(
               async () => await ElectionAdminClient.CheckAvailabilityAsync(
                   NewValidRequest(r => r.DomainOfInfluenceId = "asdf")),
               StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestForeignDomainOfInfluenceShouldThrow()
    {
        await AssertStatus(
               async () => await ElectionAdminClient.CheckAvailabilityAsync(
                   NewValidRequest(r => r.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdKirchgemeinde)),
               StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestAvailable()
    {
        var response = await AdminClient.CheckAvailabilityAsync(
            NewValidRequest(r => r.Date = new DateTime(2050, 8, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp()));
        response.Availability.Should().Be(SharedProto.ContestDateAvailability.Available);
    }

    [Fact]
    public async Task TestSameAsPreconfiguredDate()
    {
        var response = await AdminClient.CheckAvailabilityAsync(
            NewValidRequest(r => r.Date = PreconfiguredContestDateMockedData.Date20200209.ToTimestamp()));
        response.Availability.Should().Be(SharedProto.ContestDateAvailability.SameAsPreConfiguredDate);
    }

    [Fact]
    public async Task TestCloseToOtherContestDate()
    {
        var date = ContestMockedData.StGallenEvotingContest.Date.AddDays(1).ToTimestamp();

        var response = await AdminClient.CheckAvailabilityAsync(
            NewValidRequest(r => r.Date = date));
        response.Availability.Should().Be(SharedProto.ContestDateAvailability.CloseToOtherContestDate);
    }

    [Fact]
    public async Task TestAlreadyExists()
    {
        var response = await ElectionAdminClient.CheckAvailabilityAsync(
            NewValidRequest(r => r.Date = ContestMockedData.GossauContest.Date.ToTimestamp()));
        response.Availability.Should().Be(SharedProto.ContestDateAvailability.AlreadyExists);
    }

    [Fact]
    public async Task TestExistsOnChildTenant()
    {
        var response = await ElectionAdminClient.CheckAvailabilityAsync(
            NewValidRequest(r =>
            {
                r.Date = ContestMockedData.GossauContest.Date.ToTimestamp();
                r.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen;
            }));
        response.Availability.Should().Be(SharedProto.ContestDateAvailability.ExistsOnChildTenant);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new ContestService.ContestServiceClient(channel)
            .CheckAvailabilityAsync(NewValidRequest());

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    private CheckAvailabilityRequest NewValidRequest(
        Action<CheckAvailabilityRequest>? customizer = null)
    {
        var request = new CheckAvailabilityRequest
        {
            Date = new DateTime(2020, 8, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
        };
        customizer?.Invoke(request);
        return request;
    }
}
