// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.ImportTests;

public class ImportProportionalElectionListsAndCandidatesTest : BaseImportPoliticalBusinessAuthorizationTest
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

        var listEvents = EventPublisherMock.GetPublishedEvents<ProportionalElectionListCreated>().ToList();

        foreach (var listEvent in listEvents)
        {
            listEvent.ProportionalElectionList.Id = string.Empty;
        }

        listEvents.MatchSnapshot("lists");

        var candidateCreatedEvents = EventPublisherMock.GetPublishedEvents<ProportionalElectionCandidateCreated>().ToList();

        foreach (var candCreatedEvent in candidateCreatedEvents)
        {
            candCreatedEvent.ProportionalElectionCandidate.Id = string.Empty;
            candCreatedEvent.ProportionalElectionCandidate.ProportionalElectionListId = string.Empty;
        }

        candidateCreatedEvents.MatchSnapshot("candidates");
    }

    [Fact]
    public async Task TestWithPartyShouldWork()
    {
        var request = await CreateValidRequest();
        request.Lists[0].Candidates[0].Candidate.Party.Id = DomainOfInfluenceMockedData.PartyIdGossauFLiG;
        await AdminClient.ImportProportionalElectionListsAndCandidatesAsync(request);

        var candidateCreatedEvents = EventPublisherMock.GetPublishedEvents<ProportionalElectionCandidateCreated>();
        candidateCreatedEvents
            .Count(x => x.ProportionalElectionCandidate.PartyId == DomainOfInfluenceMockedData.PartyIdGossauFLiG)
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task TestWithUnknownPartyShouldThrow()
    {
        var request = await CreateValidRequest();
        request.Lists[0].Candidates[0].Candidate.Party.Id = "e8418b54-dd0f-4d89-9e47-68b57abf99e8";
        await AssertStatus(
            async () => await AdminClient.ImportProportionalElectionListsAndCandidatesAsync(request),
            StatusCode.InvalidArgument,
            "Party with id e8418b54-dd0f-4d89-9e47-68b57abf99e8 referenced by candidate 1a/1 not found");
    }

    [Fact]
    public async Task TestWithPartyOfUnrelatedDoiShouldThrow()
    {
        var request = await CreateValidRequest();
        request.Lists[0].Candidates[0].Candidate.Party.Id = DomainOfInfluenceMockedData.PartyIdKirchgemeindeEVP;
        await AssertStatus(
            async () => await AdminClient.ImportProportionalElectionListsAndCandidatesAsync(request),
            StatusCode.InvalidArgument,
            $"Party with id {DomainOfInfluenceMockedData.PartyIdKirchgemeindeEVP} referenced by candidate 1a/1 not found");
    }

    [Fact]
    public async Task TestWithDuplicatedCandidateIdShouldThrow()
    {
        var request = await CreateValidRequest();
        request.Lists[0].Candidates[0].Candidate.Id = request.Lists[0].Candidates[1].Candidate.Id;
        await AssertStatus(
            async () => await AdminClient.ImportProportionalElectionListsAndCandidatesAsync(request),
            StatusCode.InvalidArgument,
            "This id is not unique");
    }

    [Fact]
    public async Task TestMultipleImports()
    {
        var request = await CreateValidRequest();

        await AdminClient.ImportProportionalElectionListsAndCandidatesAsync(request);
        var deleteListUnionEvents1 = EventPublisherMock.GetPublishedEvents<ProportionalElectionListUnionDeleted>().ToList();
        var deleteListEvents1 = EventPublisherMock.GetPublishedEvents<ProportionalElectionListDeleted>().ToList();
        var createListEvents1 = EventPublisherMock.GetPublishedEvents<ProportionalElectionListCreated>().ToList();
        var createCandidateEvents1 = EventPublisherMock.GetPublishedEvents<ProportionalElectionCandidateCreated>().ToList();
        var createListUnionEvents1 = EventPublisherMock.GetPublishedEvents<ProportionalElectionListUnionCreated>().ToList();
        var updateListUnionEntriesEvents1 = EventPublisherMock.GetPublishedEvents<ProportionalElectionListUnionEntriesUpdated>().ToList();

        var nrOfEvents1 = await Publish(0, deleteListUnionEvents1);
        nrOfEvents1 = await Publish(nrOfEvents1, deleteListEvents1);
        nrOfEvents1 = await Publish(nrOfEvents1, createListEvents1);
        nrOfEvents1 = await Publish(nrOfEvents1, createCandidateEvents1);
        nrOfEvents1 = await Publish(nrOfEvents1, createListUnionEvents1);
        await Publish(nrOfEvents1, updateListUnionEntriesEvents1);
        EventPublisherMock.Clear();

        await AdminClient.ImportProportionalElectionListsAndCandidatesAsync(request);
        var deleteListUnionEvents2 = EventPublisherMock.GetPublishedEvents<ProportionalElectionListUnionDeleted>().ToList();
        var deleteListEvents2 = EventPublisherMock.GetPublishedEvents<ProportionalElectionListDeleted>().ToList();
        var createListEvents2 = EventPublisherMock.GetPublishedEvents<ProportionalElectionListCreated>().ToList();
        var createCandidateEvents2 = EventPublisherMock.GetPublishedEvents<ProportionalElectionCandidateCreated>().ToList();
        var createListUnionEvents2 = EventPublisherMock.GetPublishedEvents<ProportionalElectionListUnionCreated>().ToList();
        var updateListUnionEntriesEvents2 = EventPublisherMock.GetPublishedEvents<ProportionalElectionListUnionEntriesUpdated>().ToList();

        createListEvents1.Should().HaveCountGreaterThan(0);
        deleteListEvents1.Should().HaveCountGreaterThan(0);
        createCandidateEvents1.Should().HaveCountGreaterThan(0);
        createListUnionEvents1.Should().HaveCountGreaterThan(0);
        updateListUnionEntriesEvents1.Should().HaveCountGreaterThan(0);

        createListEvents2.Should().HaveCount(createListEvents1.Count);
        deleteListEvents2.Should().HaveCount(request.Lists.Count);
        createCandidateEvents2.Should().HaveCount(createCandidateEvents1.Count);
        createListUnionEvents2.Should().HaveCount(createListUnionEvents1.Count);
        updateListUnionEntriesEvents2.Should().HaveCount(updateListUnionEntriesEvents1.Count);

        var nrOfEvents2 = await Publish(0, deleteListUnionEvents2);
        nrOfEvents2 = await Publish(nrOfEvents2, deleteListEvents2);
        nrOfEvents2 = await Publish(nrOfEvents2, createListEvents2);
        nrOfEvents2 = await Publish(nrOfEvents2, createCandidateEvents2);
        nrOfEvents2 = await Publish(nrOfEvents2, createListUnionEvents2);
        await Publish(nrOfEvents2, updateListUnionEntriesEvents2);

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

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
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

    private async Task<long> Publish<TEvent>(long nrOfEvents, ICollection<TEvent> events)
        where TEvent : IMessage<TEvent>
    {
        await TestEventPublisher.Publish(nrOfEvents, events.ToArray());
        return nrOfEvents + events.Count;
    }
}
