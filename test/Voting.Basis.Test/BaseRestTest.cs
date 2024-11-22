// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
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
using Xunit;

namespace Voting.Basis.Test;

public abstract class BaseRestTest : RestAuthorizationBaseTest<TestApplicationFactory, TestStartup>
{
    private readonly Lazy<HttpClient> _adminClient;
    private readonly Lazy<HttpClient> _cantonAdminClient;
    private readonly Lazy<HttpClient> _electionAdminClient;
    private readonly Lazy<HttpClient> _zurichCantonAdminClient;

    protected BaseRestTest(TestApplicationFactory factory)
        : base(factory)
    {
        ResetDb();

        TestEventPublisher = GetService<TestEventPublisher>();
        EventPublisherMock = GetService<EventPublisherMock>();
        AggregateRepositoryMock = GetService<AggregateRepositoryMock>();
        EventPublisherMock.Clear();
        AggregateRepositoryMock.Clear();

        _adminClient = new Lazy<HttpClient>(() => CreateHttpClient(Roles.Admin));
        _cantonAdminClient = new Lazy<HttpClient>(() => CreateHttpClient(Roles.CantonAdmin));
        _electionAdminClient = new Lazy<HttpClient>(() => CreateHttpClient(Roles.ElectionAdmin));
        _zurichCantonAdminClient = new Lazy<HttpClient>(() => CreateHttpClient(true, "zürich-sec-id", roles: Roles.CantonAdmin));
    }

    protected EventPublisherMock EventPublisherMock { get; }

    protected AggregateRepositoryMock AggregateRepositoryMock { get; }

    protected TestEventPublisher TestEventPublisher { get; }

    protected HttpClient AdminClient => _adminClient.Value;

    protected HttpClient CantonAdminClient => _cantonAdminClient.Value;

    protected HttpClient ElectionAdminClient => _electionAdminClient.Value;

    protected HttpClient ZurichCantonAdminClient => _zurichCantonAdminClient.Value;

    /// <summary>
    /// Authorized roles should have access to the method.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task AuthorizedRolesShouldHaveAccess()
    {
        foreach (var role in AuthorizedRoles())
        {
            var response = await AuthorizationTestCall(CreateHttpClient(role == NoRole ? Array.Empty<string>() : new[] { role }));
            response.IsSuccessStatusCode.Should().BeTrue($"{role} should have access");
        }
    }

    protected abstract IEnumerable<string> AuthorizedRoles();

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        return Roles
            .All()
            .Append(NoRole)
            .Except(AuthorizedRoles());
    }

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
