// (c) Copyright 2022 by Abraxas Informatik AG
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

namespace Voting.Basis.Test.CountingCircleTests;

public class CountingCircleDeleteTest : BaseGrpcTest<CountingCircleService.CountingCircleServiceClient>
{
    private const string IdNotFound = "eae2cfaf-c787-48b9-a108-c975b0addddd";
    private const string IdInvalid = "eae2xxxx";

    public CountingCircleDeleteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await CountingCircleMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestInvalidGuid()
        => await AssertStatus(
            async () => await AdminClient.DeleteAsync(new DeleteCountingCircleRequest
            {
                Id = IdInvalid,
            }),
            StatusCode.InvalidArgument);

    [Fact]
    public async Task TestNotFound()
        => await AssertStatus(
            async () => await AdminClient.DeleteAsync(new DeleteCountingCircleRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.NotFound);

    [Fact]
    public async Task Test()
    {
        await AdminClient.DeleteAsync(new DeleteCountingCircleRequest
        {
            Id = CountingCircleMockedData.IdUzwil,
        });
        var eventData = EventPublisherMock.GetSinglePublishedEvent<CountingCircleDeleted>();

        eventData.CountingCircleId.Should().Be(CountingCircleMockedData.IdUzwil);
        eventData.MatchSnapshot();
    }

    [Fact]
    public async Task TestAggregate()
    {
        var idUzwil = CountingCircleMockedData.IdUzwil;
        await TestEventPublisher.Publish(new CountingCircleDeleted
        {
            CountingCircleId = idUzwil,
            EventInfo = GetMockedEventInfo(),
        });

        var idGuid = Guid.Parse(idUzwil);
        (await RunOnDb(db => db.CountingCircles.CountAsync(cc => cc.Id == idGuid)))
            .Should().Be(0);
        (await RunOnDb(db => db.DomainOfInfluenceCountingCircles.CountAsync(dc => dc.CountingCircleId == idGuid)))
            .Should().Be(0);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        var idUzwil = CountingCircleMockedData.IdUzwil;

        await new CountingCircleService.CountingCircleServiceClient(channel)
            .DeleteAsync(new DeleteCountingCircleRequest { Id = idUzwil });
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return Roles.ElectionAdmin;
    }
}
