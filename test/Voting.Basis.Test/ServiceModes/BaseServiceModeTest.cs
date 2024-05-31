// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Shared.V1;
using FluentAssertions;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Configuration;
using Voting.Basis.Data;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Mocks;
using Xunit;

namespace Voting.Basis.Test.ServiceModes;

public abstract class BaseServiceModeTest<TFactory> : BaseTest<TFactory, TestStartup>
    where TFactory : BaseTestApplicationFactory<TestStartup>
{
    private readonly ServiceMode _serviceMode;

    protected BaseServiceModeTest(TFactory factory, ServiceMode serviceMode)
        : base(factory)
    {
        _serviceMode = serviceMode;
    }

    [Fact]
    public async Task WriteEndpointShouldWorkIfPublisher()
    {
        if (!_serviceMode.HasFlag(ServiceMode.Publisher))
        {
            return;
        }

        var eventPublisherMock = GetService<EventPublisherMock>();
        eventPublisherMock.Clear();

        DatabaseUtil.Truncate(GetService<DataContext>());
        using var channel = CreateGrpcChannel(Roles.Admin);
        var client = new CountingCircleService.CountingCircleServiceClient(channel);
        var resp = await client
            .CreateAsync(
                new()
                {
                    Bfs = "123",
                    Code = "123",
                    Name = "test",
                    ResponsibleAuthority = new() { SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id },
                    ContactPersonAfterEvent = new(),
                    ContactPersonDuringEvent = new(),
                    ContactPersonSameDuringEventAsAfter = true,
                    Canton = DomainOfInfluenceCanton.Sg,
                });
        resp.Id.Should().NotBeNull();

        var createdEvent = eventPublisherMock.GetSinglePublishedEvent<CountingCircleCreated>();
        Guid.Parse(createdEvent.CountingCircle.Id).Should().Be(resp.Id);
    }

    [Fact]
    public async Task WriteEndpointShouldThrowIfNotPublisher()
    {
        if (_serviceMode.HasFlag(ServiceMode.Publisher))
        {
            return;
        }

        using var channel = CreateGrpcChannel(Roles.Admin);
        var client = new CountingCircleService.CountingCircleServiceClient(channel);
        var ex = await Assert.ThrowsAnyAsync<RpcException>(async () => await client
            .CreateAsync(
                new()
                {
                    Bfs = "123",
                    Code = "123",
                    Name = "test",
                    ResponsibleAuthority = new() { SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id },
                    ContactPersonAfterEvent = new(),
                    ContactPersonDuringEvent = new(),
                    ContactPersonSameDuringEventAsAfter = true,
                }));
        ex.StatusCode.Should().Be(StatusCode.Unimplemented);
    }

    [Fact]
    public async Task ReadEndpointShouldWorkIfPublisher()
    {
        if (!_serviceMode.HasFlag(ServiceMode.Publisher))
        {
            return;
        }

        DatabaseUtil.Truncate(GetService<DataContext>());
        await CountingCircleMockedData.Seed(RunScoped);
        using var channel = CreateGrpcChannel(Roles.Admin);
        var client = new CountingCircleService.CountingCircleServiceClient(channel);
        var cc = await client.GetAsync(new() { Id = CountingCircleMockedData.IdBund });
        cc.Id.Should().Be(CountingCircleMockedData.IdBund);
    }

    [Fact]
    public async Task ReadEndpointShouldThrowIfNotPublisher()
    {
        if (_serviceMode.HasFlag(ServiceMode.Publisher))
        {
            return;
        }

        using var channel = CreateGrpcChannel(Roles.Admin);
        var client = new CountingCircleService.CountingCircleServiceClient(channel);
        var ex = await Assert.ThrowsAsync<RpcException>(async () => await client.GetAsync(new() { Id = CountingCircleMockedData.IdBund }));
        ex.StatusCode.Should().Be(StatusCode.Unimplemented);
    }

    [Fact]
    public async Task EventProcessingShouldWorkIfEventProcessor()
    {
        if (!_serviceMode.HasFlag(ServiceMode.EventProcessor))
        {
            return;
        }

        var testPublisher = GetService<TestEventPublisher>();

        DatabaseUtil.Truncate(GetService<DataContext>());
        var id = Guid.Parse("7ef5e239-be6f-4d4c-89c9-b3a39cdc41ff");
        await testPublisher.Publish(new CountingCircleCreated
        {
            CountingCircle = new()
            {
                Bfs = "123",
                Code = "123",
                Id = id.ToString(),
                Name = "test",
                ResponsibleAuthority = new(),
                ContactPersonAfterEvent = new(),
                ContactPersonDuringEvent = new(),
                ContactPersonSameDuringEventAsAfter = true,
            },
            EventInfo = new()
            {
                Timestamp = MockedClock.UtcNowTimestamp,
                User = new() { Id = "Service-User" },
                Tenant = new() { Id = "Tenant" },
            },
        });

        var cc = await GetService<DataContext>().CountingCircles.FirstAsync(cc => cc.Id == id);
        cc.Bfs.Should().Be("123");
    }

    [Fact(Skip = "Metric endpoint test is not working properly with dedicated prometheus metric server port (ref: VOTING-4006)")]
    public async Task MetricsEndpointShouldWork()
    {
        var client = CreateHttpClient(false);
        var response = await client.GetPrometheusMetricsAsync();
        response
            .Should()
            .NotBeEmpty();
    }

    [Fact]
    public async Task HealthEndpointShouldWork()
    {
        var client = CreateHttpClient(false);
        await client.GetStringAsync("/healthz");
    }
}
