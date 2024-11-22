// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Xml.Schema;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Controllers.Models;
using Voting.Basis.Core.Auth;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Ech;
using Voting.Lib.Ech.Ech0157_4_0.Schemas;
using Voting.Lib.Ech.Ech0159_4_0.Schemas;
using Voting.Lib.Testing.Utils;
using Voting.Lib.VotingExports.Repository.Basis;
using Xunit;

namespace Voting.Basis.Test.ExportTests;

public class ExportGenerateTest : BaseRestTest
{
    private const string IdNotFound = "bfe2cfaf-c787-48b9-a108-c975b0addddd";

    public ExportGenerateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await VoteMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped, false);
        await ProportionalElectionMockedData.Seed(RunScoped, false);
    }

    [Fact]
    public async Task TestAsAdminShouldReturnOk()
    {
        var response = await AssertStatus(
            () => AdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(ContestMockedData.IdBundContest),
                Key = BasisXmlContestTemplates.Ech0157And0159.Key,
            }),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Zip);

        await ZipEntriesShouldMatchSnapshot(response, nameof(TestAsAdminShouldReturnOk));
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturnOk()
    {
        var response = await AssertStatus(
            () => ElectionAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(ContestMockedData.IdBundContest),
                Key = BasisXmlContestTemplates.Ech0157And0159.Key,
            }),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Zip);

        await ZipEntriesShouldMatchSnapshot(response, nameof(TestAsElectionAdminShouldReturnOk));
    }

    [Fact]
    public async Task TestVoteEch0159()
    {
        var response = await AssertStatus(
            () => ElectionAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(VoteMockedData.IdGossauVoteInContestGossau),
                Key = BasisXmlVoteTemplates.Ech0159.Key,
            }),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Xml);

        var xml = await response.Content.ReadAsStringAsync();
        VerifyXml(xml, nameof(TestVoteEch0159), Ech0159Schemas.LoadEch0159Schemas());
    }

    [Fact]
    public async Task TestVoteEch0159_TestDeliveryFlag()
    {
        await RunOnDb(async db =>
        {
            var contest = await db.Contests.AsTracking().SingleAsync(c => c.Id == ContestMockedData.GossauContest.Id);
            contest.State = ContestState.Active;
            await db.SaveChangesAsync();
        });

        var response = await AssertStatus(
            () => ElectionAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(VoteMockedData.IdGossauVoteInContestGossau),
                Key = BasisXmlVoteTemplates.Ech0159.Key,
            }),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Xml);

        var xml = await response.Content.ReadAsStringAsync();
        var schemaSet = Ech0159Schemas.LoadEch0159Schemas();
        var delivery = new EchDeserializer().DeserializeXml<Ech0159_4_0.Delivery>(xml, schemaSet);

        delivery.DeliveryHeader.TestDeliveryFlag.Should().BeFalse();
    }

    [Fact]
    public async Task TestVoteEch0159VariantQuestionsOnMultipleBallots()
    {
        var response = await AssertStatus(
            () => ZurichCantonAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(VoteMockedData.IdZurichVoteInContestZurich),
                Key = BasisXmlVoteTemplates.Ech0159.Key,
            }),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Xml);

        var xml = await response.Content.ReadAsStringAsync();
        VerifyXml(xml, nameof(TestVoteEch0159VariantQuestionsOnMultipleBallots), Ech0159Schemas.LoadEch0159Schemas());
    }

    [Fact]
    public async Task TestMajorityElectionEch0157()
    {
        await RunOnDb(async db =>
        {
            var doi = await db.DomainOfInfluences.AsTracking().SingleAsync(c => c.Id == DomainOfInfluenceMockedData.GuidGossau);
            doi.CantonDefaults.Canton = DomainOfInfluenceCanton.Tg;
            await db.SaveChangesAsync();
        });

        var response = await AssertStatus(
            () => ElectionAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(MajorityElectionMockedData.IdGossauMajorityElectionInContestGossau),
                Key = BasisXmlMajorityElectionTemplates.Ech0157.Key,
            }),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Xml);

        var xml = await response.Content.ReadAsStringAsync();
        VerifyXml(xml, nameof(TestMajorityElectionEch0157), Ech0157Schemas.LoadEch0157Schemas());
    }

    [Fact]
    public async Task TestMajorityElectionEch0157_TestDeliveryFlag()
    {
        await RunOnDb(async db =>
        {
            var doi = await db.DomainOfInfluences.AsTracking().SingleAsync(c => c.Id == DomainOfInfluenceMockedData.GuidGossau);
            doi.CantonDefaults.Canton = DomainOfInfluenceCanton.Tg;

            var contest = await db.Contests.AsTracking().SingleAsync(c => c.Id == ContestMockedData.GossauContest.Id);
            contest.State = ContestState.Active;

            await db.SaveChangesAsync();
        });

        var response = await AssertStatus(
            () => ElectionAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(MajorityElectionMockedData.IdGossauMajorityElectionInContestGossau),
                Key = BasisXmlMajorityElectionTemplates.Ech0157.Key,
            }),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Xml);

        var xml = await response.Content.ReadAsStringAsync();
        var schemaSet = Ech0157Schemas.LoadEch0157Schemas();
        var delivery = new EchDeserializer().DeserializeXml<Ech0157_4_0.Delivery>(xml, schemaSet);

        delivery.DeliveryHeader.TestDeliveryFlag.Should().BeFalse();
    }

    [Fact]
    public async Task TestProportionalElectionEch0157()
    {
        var response = await AssertStatus(
            () => ElectionAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund),
                Key = BasisXmlProportionalElectionTemplates.Ech0157.Key,
            }),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Xml);

        var xml = await response.Content.ReadAsStringAsync();
        VerifyXml(xml, nameof(TestProportionalElectionEch0157), Ech0157Schemas.LoadEch0157Schemas());
    }

    [Fact]
    public async Task TestProportionalElectionEch0157_TestDeliveryFlag()
    {
        await RunOnDb(async db =>
        {
            var contest = await db.Contests.AsTracking().SingleAsync(c => c.Id == ContestMockedData.BundContest.Id);
            contest.State = ContestState.Active;
            await db.SaveChangesAsync();
        });

        var response = await AssertStatus(
            () => ElectionAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund),
                Key = BasisXmlProportionalElectionTemplates.Ech0157.Key,
            }),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Xml);

        var xml = await response.Content.ReadAsStringAsync();
        var schemaSet = Ech0157Schemas.LoadEch0157Schemas();
        var delivery = new EchDeserializer().DeserializeXml<Ech0157_4_0.Delivery>(xml, schemaSet);

        delivery.DeliveryHeader.TestDeliveryFlag.Should().BeFalse();
    }

    [Fact]
    public async Task TestUnknownKeyShouldThrow()
    {
        await AssertStatus(
            () => ElectionAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(VoteMockedData.IdBundVoteInContestStGallen),
                Key = "test",
            }),
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TestIdNotFound()
    {
        await AssertStatus(
            () => ElectionAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(IdNotFound),
                Key = BasisXmlVoteTemplates.Ech0159.Key,
            }),
            HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TestForeignContest()
    {
        await AssertStatus(
            () => ElectionAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(ContestMockedData.IdKirche),
                Key = BasisXmlContestTemplates.Ech0157And0159.Key,
            }),
            HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TestParentVote()
    {
        await AssertStatus(
            () => ElectionAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(VoteMockedData.IdBundVoteInContestStGallen),
                Key = BasisXmlVoteTemplates.Ech0159.Key,
            }),
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TestForeignVote()
    {
        await AssertStatus(
            () => ElectionAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(VoteMockedData.IdKircheVoteInContestKircheWithoutChilds),
                Key = BasisXmlVoteTemplates.Ech0159.Key,
            }),
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TestParentMajorityElection()
    {
        await AssertStatus(
            () => ElectionAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(MajorityElectionMockedData.IdBundMajorityElectionInContestStGallen),
                Key = BasisXmlMajorityElectionTemplates.Ech0157.Key,
            }),
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TestForeignMajorityElection()
    {
        await AssertStatus(
            () => ElectionAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(MajorityElectionMockedData.IdKircheMajorityElectionInContestKirche),
                Key = BasisXmlMajorityElectionTemplates.Ech0157.Key,
            }),
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TestParentProportionalElection()
    {
        await AssertStatus(
            () => ElectionAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(ProportionalElectionMockedData.IdBundProportionalElectionInContestStGallen),
                Key = BasisXmlProportionalElectionTemplates.Ech0157.Key,
            }),
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TestForeignProportionalElection()
    {
        await AssertStatus(
            () => ElectionAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(ProportionalElectionMockedData.IdKircheProportionalElectionInContestKirche),
                Key = BasisXmlProportionalElectionTemplates.Ech0157.Key,
            }),
            HttpStatusCode.Forbidden);
    }

    protected override Task<HttpResponseMessage> AuthorizationTestCall(HttpClient httpClient)
    {
        return httpClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
        {
            EntityId = Guid.Parse(ContestMockedData.IdBundContest),
            Key = BasisXmlContestTemplates.Ech0157And0159.Key,
        });
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    private async Task ZipEntriesShouldMatchSnapshot(HttpResponseMessage response, string testName)
    {
        await using var zipStream = await response.Content.ReadAsStreamAsync();
        using var zipArchive = new ZipArchive(zipStream);

        zipArchive.Entries.Count.Should().Be(3);

        foreach (var entry in zipArchive.Entries)
        {
            using var streamReader = new StreamReader(entry.Open());
            var content = await streamReader.ReadToEndAsync();

            if (entry.Name.Contains("_votes"))
            {
                VerifyXml(content, $"{testName}_votes", Ech0159Schemas.LoadEch0159Schemas());
            }
            else if (entry.Name.Contains("_proportional_elections"))
            {
                VerifyXml(content, $"{testName}_proportional_elections", Ech0157Schemas.LoadEch0157Schemas());
            }
            else if (entry.Name.Contains("_majority_elections"))
            {
                VerifyXml(content, $"{testName}_majority_elections", Ech0157Schemas.LoadEch0157Schemas());
            }
            else
            {
                throw new InvalidOperationException($"Unknown export entry {entry.Name}");
            }
        }
    }

    private void VerifyXml(string xml, string fileName, XmlSchemaSet schemaSet)
    {
        XmlUtil.ValidateSchema(xml, schemaSet);
        xml = XmlUtil.FormatTestXml(xml);
        xml.MatchRawTextSnapshot("ExportTests", "_snapshots", fileName + ".xml");
    }
}
