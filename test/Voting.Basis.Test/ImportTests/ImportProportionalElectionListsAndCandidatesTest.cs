// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
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
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.ImportTests;

public class ImportProportionalElectionListsAndCandidatesTest : BaseImportTest
{
    public ImportProportionalElectionListsAndCandidatesTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await ProportionalElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestShouldWork()
    {
        var request = await CreateValidRequest();
        await AdminClient.ImportProportionalElectionListsAndCandidatesAsync(request);
    }

    [Fact]
    public async Task TestNotOwnDomainOfInfluenceShouldThrow()
    {
        var request = await CreateValidRequest();
        var adminClientDifferentTenant = new ImportService.ImportServiceClient(
            CreateGrpcChannel(tenant: DomainOfInfluenceMockedData.IdStGallen, roles: Roles.Admin));

        await AssertStatus(
            async () => await adminClientDifferentTenant.ImportProportionalElectionListsAndCandidatesAsync(request),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestMultipleImports()
    {
        var request = await CreateValidRequest();

        await AdminClient.ImportProportionalElectionListsAndCandidatesAsync(request);
        var createListEvents1 = EventPublisherMock.GetPublishedEvents<ProportionalElectionListCreated>().ToList();
        var createCandidateEvents1 = EventPublisherMock.GetPublishedEvents<ProportionalElectionCandidateCreated>().ToList();
        var createListUnionEvents1 = EventPublisherMock.GetPublishedEvents<ProportionalElectionListUnionCreated>().ToList();
        var updateListUnionEntriesEvents1 = EventPublisherMock.GetPublishedEvents<ProportionalElectionListUnionEntriesUpdated>().ToList();

        await TestEventPublisher.Publish(createListEvents1.ToArray());
        await TestEventPublisher.Publish(5, createCandidateEvents1.ToArray());
        await TestEventPublisher.Publish(16, createListUnionEvents1.ToArray());
        await TestEventPublisher.Publish(17, updateListUnionEntriesEvents1.ToArray());
        EventPublisherMock.Clear();

        await AdminClient.ImportProportionalElectionListsAndCandidatesAsync(request);
        var createListEvents2 = EventPublisherMock.GetPublishedEvents<ProportionalElectionListCreated>().ToList();
        var createCandidateEvents2 = EventPublisherMock.GetPublishedEvents<ProportionalElectionCandidateCreated>().ToList();
        var createListUnionEvents2 = EventPublisherMock.GetPublishedEvents<ProportionalElectionListUnionCreated>().ToList();
        var updateListUnionEntriesEvents2 = EventPublisherMock.GetPublishedEvents<ProportionalElectionListUnionEntriesUpdated>().ToList();

        createListEvents1.Should().HaveCountGreaterThan(0);
        createCandidateEvents1.Should().HaveCountGreaterThan(0);
        createListUnionEvents1.Should().HaveCountGreaterThan(0);
        updateListUnionEntriesEvents1.Should().HaveCountGreaterThan(0);

        createListEvents2.Should().HaveCount(0);
        createCandidateEvents2.Should().HaveCount(0);
        createListUnionEvents2.Should().HaveCount(0);
        updateListUnionEntriesEvents2.Should().HaveCount(0);

        var lists = await RunOnDb(db => db.ProportionalElectionLists
            .Where(l => l.ProportionalElectionId == Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestGossau))
            .Include(l => l.ProportionalElectionCandidates)
            .OrderBy(c => c.Position)
            .ToListAsync());

        var listUnions = await RunOnDb(db => db.ProportionalElectionListUnions
            .Where(l => l.ProportionalElectionId == Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestGossau))
            .OrderBy(c => c.Position)
            .ToListAsync());

        foreach (var list in lists)
        {
            foreach (var candidate in list.ProportionalElectionCandidates)
            {
                candidate.Id = Guid.Empty;
                candidate.ProportionalElectionListId = Guid.Empty;
            }

            list.ProportionalElectionCandidates = list.ProportionalElectionCandidates.OrderBy(c => c.Position).ToList();
        }

        lists.MatchSnapshot("lists", c => c.Id);
        listUnions.MatchSnapshot("listUnions", c => c.Id, c => c.ProportionalElectionRootListUnionId!);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ImportService.ImportServiceClient(channel)
            .ImportProportionalElectionListsAndCandidatesAsync(await CreateValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    private async Task<ImportProportionalElectionListsAndCandidatesRequest> CreateValidRequest()
    {
        var contest = await AdminClient.ResolveImportFileAsync(new ResolveImportFileRequest
        {
            ImportType = SharedProto.ImportType.Ech157,
            FileContent = await GetTestEch0157File(),
        });

        var proportionalElectionImport = contest.ProportionalElections[0];

        return new ImportProportionalElectionListsAndCandidatesRequest
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestGossau,
            Lists = { proportionalElectionImport.Lists },
            ListUnions = { proportionalElectionImport.ListUnions },
        };
    }
}
