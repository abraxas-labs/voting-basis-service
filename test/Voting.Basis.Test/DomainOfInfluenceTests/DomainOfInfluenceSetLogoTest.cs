// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Snapper;
using Voting.Basis.Core.Extensions;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.ObjectStorage.Testing.Mocks;
using Xunit;

namespace Voting.Basis.Test.DomainOfInfluenceTests;

public class DomainOfInfluenceSetLogoTest : BaseRestTest
{
    public DomainOfInfluenceSetLogoTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task ShouldStoreLogo()
    {
        using var content = BuildSimpleContent();
        using var resp = await AdminClient.PostAsync(BuildUrl(DomainOfInfluenceMockedData.IdStGallen), content);
        resp.EnsureSuccessStatusCode();

        var storedContent = GetService<ObjectStorageClientMock>()
            .Get("voting", "domain-of-influence-logos/v1/" + DomainOfInfluenceMockedData.IdStGallen);
        Encoding.UTF8.GetString(storedContent)
            .Should()
            .StartWith("<svg height=");

        EventPublisherMock
            .GetSinglePublishedEvent<DomainOfInfluenceLogoUpdated>()
            .ShouldMatchSnapshot();
    }

    [Fact]
    public async Task ShouldStoreLogoAsElectionAdmin()
    {
        using var content = BuildSimpleContent();
        using var resp = await ElectionAdminClient.PostAsync(BuildUrl(DomainOfInfluenceMockedData.IdStGallen), content);
        resp.EnsureSuccessStatusCode();

        EventPublisherMock
            .GetSinglePublishedEvent<DomainOfInfluenceLogoUpdated>()
            .DomainOfInfluenceId
            .Should()
            .Be(DomainOfInfluenceMockedData.IdStGallen);
    }

    [Fact]
    public async Task ShouldThrowAsElectionAdminOtherTenant()
    {
        using var content = BuildSimpleContent();
        using var resp = await ElectionAdminClient.PostAsync(BuildUrl(DomainOfInfluenceMockedData.IdBund), content);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldThrowMismatchedContentType()
    {
        using var content = BuildSimpleContent("application/pdf");
        using var resp = await ElectionAdminClient.PostAsync(BuildUrl(DomainOfInfluenceMockedData.IdStGallen), content);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await CheckErrorResponse(
            resp,
            "ValidationException",
            "File extensions differ. From file name: svg, from content type: pdf, guessed from content: svg");
    }

    [Fact]
    public async Task ShouldThrowMismatchedFileName()
    {
        using var content = BuildSimpleContent(fileName: "test.gif");
        using var resp = await ElectionAdminClient.PostAsync(BuildUrl(DomainOfInfluenceMockedData.IdStGallen), content);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await CheckErrorResponse(
            resp,
            "ValidationException",
            "File extensions differ. From file name: gif, from content type: svg, guessed from content: svg");
    }

    [Fact]
    public async Task ShouldThrowNotAllowedFileExtension()
    {
        var logoContent = new ByteArrayContent(Convert.FromBase64String("R0lGODlhAQABAIAAAAUEBAAAACwAAAAAAQABAAACAkQBADs="));
        logoContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/gif");
        using var content = new MultipartFormDataContent();
        content.Add(logoContent, "logo", "logo.gif");

        using var resp = await ElectionAdminClient.PostAsync(BuildUrl(DomainOfInfluenceMockedData.IdStGallen), content);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await CheckErrorResponse(
            resp,
            "ValidationException",
            "File extension gif is not allowed for logo uploads");
    }

    [Fact]
    public async Task ProcessorShouldWork()
    {
        var logoRef = "v1/" + DomainOfInfluenceMockedData.IdStGallen;
        await TestEventPublisher.Publish(new DomainOfInfluenceLogoUpdated
        {
            EventInfo = new EventInfo
            {
                Timestamp = new Timestamp
                {
                    Seconds = 1594980476,
                },
                Tenant = SecureConnectTestDefaults.MockedTenantDefault.ToEventInfoTenant(),
                User = SecureConnectTestDefaults.MockedUserDefault.ToEventInfoUser(),
            },
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
            LogoRef = logoRef,
        });

        var doi = await RunOnDb(db => db.DomainOfInfluences.FirstAsync(x => x.Id == DomainOfInfluenceMockedData.GuidStGallen));
        doi.HasLogo.Should().BeTrue();
        doi.LogoRef.Should().Be(logoRef);
    }

    protected override async Task<HttpResponseMessage> AuthorizationTestCall(HttpClient httpClient)
    {
        using var content = BuildSimpleContent();
        return await httpClient.PostAsync(BuildUrl(DomainOfInfluenceMockedData.IdStGallen), content);
    }

    private static HttpContent BuildSimpleContent(string? contentType = null, string? fileName = null)
    {
        var logoContent =
            new StringContent(
                @"<svg height=""100"" width=""100""><circle cx=""50"" cy=""50"" r=""40"" stroke=""black"" stroke-width=""3"" fill=""red"" /></svg>",
                Encoding.UTF8,
                contentType ?? "image/svg");
        var data = new MultipartFormDataContent();
        data.Add(logoContent, "logo", fileName ?? "logo.svg");
        return data;
    }

    private static string BuildUrl(string id)
        => $"api/domain-of-influences/{id}/logo";
}
