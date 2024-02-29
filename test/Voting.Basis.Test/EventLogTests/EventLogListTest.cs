// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.EventLogTests;

public class EventLogListTest : BaseGrpcTest<EventLogService.EventLogServiceClient>
{
    public EventLogListTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await EventLogMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestAsAdminShouldReturnAll()
    {
        var response = await AdminClient.ListAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturnOnlyOwnTenantEvents()
    {
        var response = await ElectionAdminClient.ListAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowInvalidPage()
    {
        await AssertStatus(
            async () => await AdminClient.ListAsync(NewValidRequest(r => r.Pageable.Page = 0)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowPageSizeToSmall()
    {
        await AssertStatus(
            async () => await AdminClient.ListAsync(NewValidRequest(r => r.Pageable.PageSize = 0)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowPageSizeToLarge()
    {
        await AssertStatus(
            async () => await AdminClient.ListAsync(NewValidRequest(r => r.Pageable.PageSize = 101)),
            StatusCode.InvalidArgument);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new EventLogService.EventLogServiceClient(channel)
            .ListAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    private ListEventLogsRequest NewValidRequest(
        Action<ListEventLogsRequest>? customizer = null)
    {
        var request = new ListEventLogsRequest
        {
            Pageable = new ProtoModels.Pageable
            {
                Page = 1,
                PageSize = 10,
            },
        };

        customizer?.Invoke(request);
        return request;
    }
}
