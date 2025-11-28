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
using Ech0157SchemasV5 = Voting.Lib.Ech.Ech0157_5_1.Schemas.Ech0157Schemas;
using Ech0159SchemasV5 = Voting.Lib.Ech.Ech0159_5_1.Schemas.Ech0159Schemas;

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
                Key = BasisXmlContestTemplates.Ech0157And0159_4_0.Key,
            }),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Zip);

        await ZipEntriesShouldMatchSnapshot(response, nameof(TestAsAdminShouldReturnOk), false);
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturnOk()
    {
        var response = await AssertStatus(
            () => ElectionAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(ContestMockedData.IdBundContest),
                Key = BasisXmlContestTemplates.Ech0157And0159_4_0.Key,
            }),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Zip);

        await ZipEntriesShouldMatchSnapshot(response, nameof(TestAsElectionAdminShouldReturnOk));
    }

    [Fact]
    public async Task TestContestCandidateListShouldReturnOk()
    {
        var response = await AssertStatus(
            () => ElectionAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(ContestMockedData.IdBundContest),
                Key = BasisCsvContestTemplates.CandidateList.Key,
            }),
            HttpStatusCode.OK);

        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Text.Csv);
        response.Content.Headers.ContentDisposition!.FileName.Should().Be("\"candidate_list_Nationalratswahlen in der Zukunft de.csv\"");
        var csv = await response.Content.ReadAsStringAsync();
        csv = csv.ReplaceLineEndings("\n");
        csv.MatchRawTextSnapshot("ExportTests", "_snapshots", nameof(TestContestCandidateListShouldReturnOk) + ".csv");
    }

    [Fact]
    public async Task TestProportionalCandidateListShouldReturnOk()
    {
        var response = await AssertStatus(
            () => ElectionAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestGossau),
                Key = BasisCsvProportionalElectionTemplates.CandidateList.Key,
            }),
            HttpStatusCode.OK);

        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Text.Csv);
        var csv = await response.Content.ReadAsStringAsync();
        csv = csv.ReplaceLineEndings("\n");
        csv.MatchRawTextSnapshot("ExportTests", "_snapshots", nameof(TestProportionalCandidateListShouldReturnOk) + ".csv");
    }

    [Fact]
    public async Task TestMajorityCandidateListShouldReturnOk()
    {
        var response = await AssertStatus(
            () => ElectionAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(MajorityElectionMockedData.IdGossauMajorityElectionInContestBund),
                Key = BasisCsvMajorityElectionTemplates.CandidateList.Key,
            }),
            HttpStatusCode.OK);

        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Text.Csv);
        var csv = await response.Content.ReadAsStringAsync();
        csv = csv.ReplaceLineEndings("\n");
        csv.MatchRawTextSnapshot("ExportTests", "_snapshots", nameof(TestMajorityCandidateListShouldReturnOk) + ".csv");
    }

    [Fact]
    public async Task TestAsElectionAdminWithEch0157And0159V5ShouldReturnOk()
    {
        var response = await AssertStatus(
            () => ElectionAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(ContestMockedData.IdBundContest),
                Key = BasisXmlContestTemplates.Ech0157And0159_5_1.Key,
            }),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Zip);

        await ZipEntriesShouldMatchSnapshot(response, nameof(TestAsElectionAdminWithEch0157And0159V5ShouldReturnOk), true);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TestOnlyEVoting(bool v5)
    {
        await RunOnDb(async db =>
        {
            var contest = await db.Contests
                .AsSplitQuery()
                .AsTracking()
                .Include(c => c.Votes)
                .Include(c => c.ProportionalElections)
                .Include(c => c.MajorityElections)
                .SingleAsync(c => c.Id == ContestMockedData.BundContest.Id);
            contest.EVoting = true;
            contest.EVotingFrom = new DateTime(2020, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            contest.EVotingTo = new DateTime(2020, 1, 2, 10, 0, 0, DateTimeKind.Utc);
            foreach (var vote in contest.Votes)
            {
                vote.EVotingApproved = false;
            }

            foreach (var election in contest.ProportionalElections)
            {
                election.EVotingApproved = false;
            }

            foreach (var election in contest.MajorityElections)
            {
                election.EVotingApproved = false;
            }

            await db.SaveChangesAsync();
        });

        var response = await AssertStatus(
            () => ElectionAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = ContestMockedData.BundContest.Id,
                Key = v5 ? BasisXmlContestTemplates.Ech0157And0159_5_1.Key : BasisXmlContestTemplates.Ech0157And0159_4_0_EVotingOnly.Key,
            }),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Zip);

        await ZipEntriesShouldMatchSnapshot(response, nameof(TestOnlyEVoting), v5);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task TestVoteEch0159(bool eVoting, bool v5)
    {
        if (eVoting)
        {
            await SetEVotingContest(ContestMockedData.IdGossau);
        }

        var response = await AssertStatus(
            () => ElectionAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(VoteMockedData.IdGossauVoteInContestGossau),
                Key = v5 ? BasisXmlVoteTemplates.Ech0159_5_1.Key : BasisXmlVoteTemplates.Ech0159_4_0.Key,
            }),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Xml);

        var xml = await response.Content.ReadAsStringAsync();
        VerifyXml(
            xml,
            BuildTestFilename(nameof(TestVoteEch0159), eVoting, v5),
            v5 ? Ech0159SchemasV5.LoadEch0159Schemas() : Ech0159Schemas.LoadEch0159Schemas());
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
                Key = BasisXmlVoteTemplates.Ech0159_4_0.Key,
            }),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Xml);

        var xml = await response.Content.ReadAsStringAsync();
        var schemaSet = Ech0159Schemas.LoadEch0159Schemas();
        var delivery = new EchDeserializer().DeserializeXml<Ech0159_4_0.Delivery>(xml, schemaSet);

        delivery.DeliveryHeader.TestDeliveryFlag.Should().BeFalse();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task TestVoteEch0159VariantQuestionsOnMultipleBallots(bool eVoting, bool v5)
    {
        if (eVoting)
        {
            await SetEVotingContest(ContestMockedData.IdZurichContest);
        }

        var response = await AssertStatus(
            () => ZurichCantonAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(VoteMockedData.IdZurichVoteInContestZurich),
                Key = v5 ? BasisXmlVoteTemplates.Ech0159_5_1.Key : BasisXmlVoteTemplates.Ech0159_4_0.Key,
            }),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Xml);

        var xml = await response.Content.ReadAsStringAsync();
        VerifyXml(
            xml,
            BuildTestFilename(nameof(TestVoteEch0159VariantQuestionsOnMultipleBallots), eVoting, v5),
            v5 ? Ech0159SchemasV5.LoadEch0159Schemas() : Ech0159Schemas.LoadEch0159Schemas());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task TestMajorityElectionEch0157(bool eVoting, bool v5)
    {
        if (eVoting)
        {
            await SetEVotingContest(ContestMockedData.IdGossau);
        }

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
                Key = v5 ? BasisXmlMajorityElectionTemplates.Ech0157_5_1.Key : BasisXmlMajorityElectionTemplates.Ech0157_4_0.Key,
            }),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Xml);

        var xml = await response.Content.ReadAsStringAsync();
        VerifyXml(
            xml,
            BuildTestFilename(nameof(TestMajorityElectionEch0157), eVoting, v5),
            v5 ? Ech0157SchemasV5.LoadEch0157Schemas() : Ech0157Schemas.LoadEch0157Schemas());
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
                Key = BasisXmlMajorityElectionTemplates.Ech0157_4_0.Key,
            }),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Xml);

        var xml = await response.Content.ReadAsStringAsync();
        var schemaSet = Ech0157Schemas.LoadEch0157Schemas();
        var delivery = new EchDeserializer().DeserializeXml<Ech0157_4_0.Delivery>(xml, schemaSet);

        delivery.DeliveryHeader.TestDeliveryFlag.Should().BeFalse();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task TestProportionalElectionEch0157(bool eVoting, bool v5)
    {
        if (eVoting)
        {
            await SetEVotingContest(ContestMockedData.IdBundContest);
        }

        var response = await AssertStatus(
            () => ElectionAdminClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
            {
                EntityId = Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund),
                Key = v5 ? BasisXmlProportionalElectionTemplates.Ech0157_5_1.Key : BasisXmlProportionalElectionTemplates.Ech0157_4_0.Key,
            }),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Xml);

        var xml = await response.Content.ReadAsStringAsync();
        VerifyXml(
            xml,
            BuildTestFilename(nameof(TestProportionalElectionEch0157), eVoting, v5),
            v5 ? Ech0157SchemasV5.LoadEch0157Schemas() : Ech0157Schemas.LoadEch0157Schemas());
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
                Key = BasisXmlProportionalElectionTemplates.Ech0157_4_0.Key,
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
                Key = BasisXmlVoteTemplates.Ech0159_4_0.Key,
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
                Key = BasisXmlContestTemplates.Ech0157And0159_4_0.Key,
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
                Key = BasisXmlVoteTemplates.Ech0159_4_0.Key,
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
                Key = BasisXmlVoteTemplates.Ech0159_4_0.Key,
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
                Key = BasisXmlMajorityElectionTemplates.Ech0157_4_0.Key,
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
                Key = BasisXmlMajorityElectionTemplates.Ech0157_4_0.Key,
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
                Key = BasisXmlProportionalElectionTemplates.Ech0157_4_0.Key,
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
                Key = BasisXmlProportionalElectionTemplates.Ech0157_4_0.Key,
            }),
            HttpStatusCode.Forbidden);
    }

    protected override Task<HttpResponseMessage> AuthorizationTestCall(HttpClient httpClient)
    {
        return httpClient.PostAsJsonAsync("api/exports", new GenerateExportRequest
        {
            EntityId = Guid.Parse(ContestMockedData.IdBundContest),
            Key = BasisXmlContestTemplates.Ech0157And0159_4_0.Key,
        });
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.CantonAdminReadOnly;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionAdminReadOnly;
        yield return Roles.ElectionSupporter;
    }

    private async Task ZipEntriesShouldMatchSnapshot(HttpResponseMessage response, string testName, bool v5 = false)
    {
        await using var zipStream = await response.Content.ReadAsStreamAsync();
        using var zipArchive = new ZipArchive(zipStream);

        var ech0157Schemas = v5 ? Ech0157SchemasV5.LoadEch0157Schemas() : Ech0157Schemas.LoadEch0157Schemas();
        var ech0159Schemas = v5 ? Ech0159SchemasV5.LoadEch0159Schemas() : Ech0159Schemas.LoadEch0159Schemas();
        var versionSuffix = v5 ? "_v5" : string.Empty;

        zipArchive.Entries.Count.Should().Be(3);

        foreach (var entry in zipArchive.Entries)
        {
            using var streamReader = new StreamReader(entry.Open());
            var content = await streamReader.ReadToEndAsync();

            if (entry.Name.Contains("_votes"))
            {
                VerifyXml(content, $"{testName}_votes{versionSuffix}", ech0159Schemas);
            }
            else if (entry.Name.Contains("_proportional_elections"))
            {
                VerifyXml(content, $"{testName}_proportional_elections{versionSuffix}", ech0157Schemas);
            }
            else if (entry.Name.Contains("_majority_elections"))
            {
                VerifyXml(content, $"{testName}_majority_elections{versionSuffix}", ech0157Schemas);
            }
            else
            {
                throw new InvalidOperationException($"Unknown export entry {entry.Name}");
            }
        }
    }

    private async Task SetEVotingContest(string contestId)
    {
        await RunOnDb(async db =>
        {
            var contest = await db.Contests.AsTracking().SingleAsync(c => c.Id == Guid.Parse(contestId));
            contest.EVoting = true;
            contest.EVotingFrom = new DateTime(2020, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            contest.EVotingTo = new DateTime(2020, 1, 2, 10, 0, 0, DateTimeKind.Utc);
            await db.SaveChangesAsync();
        });
    }

    private void VerifyXml(string xml, string fileName, XmlSchemaSet schemaSet)
    {
        XmlUtil.ValidateSchema(xml, schemaSet);
        xml = XmlUtil.FormatTestXml(xml);
        xml.MatchRawTextSnapshot("ExportTests", "_snapshots", fileName + ".xml");
    }

    private string BuildTestFilename(string testName, bool eVoting, bool eCounting)
        => $"{testName}{(eCounting ? "_v5" : string.Empty)}{(eVoting ? "_EVoting" : string.Empty)}";
}
