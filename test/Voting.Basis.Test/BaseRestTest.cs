// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Voting.Basis.Core.Auth;
using Voting.Basis.Data;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Testing;

namespace Voting.Basis.Test;

public abstract class BaseRestTest : RestAuthorizationBaseTest<TestApplicationFactory, TestStartup>
{
    protected BaseRestTest(TestApplicationFactory factory)
        : base(factory)
    {
        ResetDb();

        TestEventPublisher = GetService<TestEventPublisher>();
        EventPublisherMock = GetService<EventPublisherMock>();
        AggregateRepositoryMock = GetService<AggregateRepositoryMock>();
        EventPublisherMock.Clear();
        AggregateRepositoryMock.Clear();

        AdminClient = CreateHttpClient(Roles.Admin);
        ElectionAdminClient = CreateHttpClient(Roles.ElectionAdmin);
    }

    protected EventPublisherMock EventPublisherMock { get; }

    protected AggregateRepositoryMock AggregateRepositoryMock { get; }

    protected TestEventPublisher TestEventPublisher { get; }

    protected HttpClient AdminClient { get; }

    protected HttpClient ElectionAdminClient { get; }

    protected Task RunOnDb(Func<DataContext, Task> action)
        => RunScoped(action);

    protected Task<TResult> RunOnDb<TResult>(Func<DataContext, Task<TResult>> action)
        => RunScoped(action);

    protected void ResetDb()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        DatabaseUtil.Truncate(db);
    }

    protected async Task CheckErrorResponse(HttpResponseMessage resp, string title, string detail)
    {
        var problemDto = await resp.Content.ReadFromJsonAsync<ProblemDetails>()
            ?? throw new InvalidOperationException("Response does not contain ProblemDetails");
        problemDto.Title.Should().Be(title);
        problemDto.Detail.Should().Be(detail);
    }
}
