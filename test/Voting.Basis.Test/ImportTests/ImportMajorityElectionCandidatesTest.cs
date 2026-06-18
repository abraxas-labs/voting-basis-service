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
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Test.ImportTests.TestFiles;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.ImportTests;

public class ImportMajorityElectionCandidatesTest : BaseImportPoliticalBusinessAuthorizationTest
{
    private int _eventIdCounter;

    public ImportMajorityElectionCandidatesTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await MajorityElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestShouldWork()
    {
        var request = await CreateValidRequest();
        await CantonAdminClient.ImportMajorityElectionCandidatesAsync(request);
    }

    [Fact]
    public async Task TestWithV5ShouldWork()
    {
        var request = await CreateValidRequest(true);
        await CantonAdminClient.ImportMajorityElectionCandidatesAsync(request);
    }

    [Fact]
    public async Task TestWithDuplicatedCandidateIdShouldThrow()
    {
        var request = await CreateValidRequest();
        request.Candidates[0].Id = request.Candidates[1].Id;
        await AssertStatus(
            async () => await CantonAdminClient.ImportMajorityElectionCandidatesAsync(request),
            StatusCode.InvalidArgument,
            "This id is not unique");
    }

    [Fact]
    public async Task TestMultipleImports()
    {
        var request = await CreateValidRequest();

        var candidateCountBefore = await RunOnDb(db => db.MajorityElectionCandidates
            .Where(c => c.MajorityElectionId == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen))
            .CountAsync());

        await CantonAdminClient.ImportMajorityElectionCandidatesAsync(request);
        var createCandidateEvents1 = EventPublisherMock.GetPublishedEvents<MajorityElectionCandidateCreated>().ToList();
        var updateCandidateEvents1 = EventPublisherMock.GetPublishedEvents<MajorityElectionCandidateUpdated>().ToList();

        await TestEventPublisher.Publish(updateCandidateEvents1.ToArray());
        _eventIdCounter += updateCandidateEvents1.Count;
        await TestEventPublisher.Publish(createCandidateEvents1.ToArray());
        _eventIdCounter += createCandidateEvents1.Count;
        EventPublisherMock.Clear();

        var candidateCount1 = await RunOnDb(db => db.MajorityElectionCandidates
            .Where(c => c.MajorityElectionId == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen))
            .CountAsync());
        candidateCount1.Should().Be(candidateCountBefore + 3);

        await CantonAdminClient.ImportMajorityElectionCandidatesAsync(request);
        var createCandidateEvents2 = EventPublisherMock.GetPublishedEvents<MajorityElectionCandidateCreated>().ToList();
        var updateCandidateEvents2 = EventPublisherMock.GetPublishedEvents<MajorityElectionCandidateUpdated>().ToList();

        await TestEventPublisher.Publish(_eventIdCounter, updateCandidateEvents2.ToArray());
        _eventIdCounter += updateCandidateEvents2.Count;
        await TestEventPublisher.Publish(_eventIdCounter, createCandidateEvents2.ToArray());

        createCandidateEvents1.Should().HaveCount(3);
        updateCandidateEvents1.Should().HaveCount(0);
        createCandidateEvents2.Should().HaveCount(0);
        updateCandidateEvents2.Should().HaveCount(3);

        var candidates = await RunOnDb(db => db.MajorityElectionCandidates
            .Where(c => c.MajorityElectionId == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen))
            .OrderBy(c => c.Position)
            .ToListAsync());
        candidates.Should().HaveCount(candidateCountBefore + 3);
        candidates.MatchSnapshot("candidates", c => c.Id);
    }

    [Fact]
    public async Task TestDuplicatedCandidateNumber()
    {
        var request = await CreateValidRequest();
        request.Candidates[0].Number = "1";
        request.Candidates[1].Number = "1";
        request.Candidates[2].Number = "1";

        await CantonAdminClient.ImportMajorityElectionCandidatesAsync(request);

        var createCandidateEvents = EventPublisherMock.GetPublishedEvents<MajorityElectionCandidateCreated>().ToList();
        createCandidateEvents.Count.Should().Be(3);
        await TestEventPublisher.Publish(_eventIdCounter, createCandidateEvents.ToArray());

        var updateCandidateEvents = EventPublisherMock.GetPublishedEvents<MajorityElectionCandidateUpdated>().ToList();
        updateCandidateEvents.Count.Should().Be(0);

        // The first candidate keeps the number 1, since it is unique
        createCandidateEvents[0].MajorityElectionCandidate.Number.Should().Be("1");

        // The others are reassigned to their position value
        createCandidateEvents[1].MajorityElectionCandidate.Number.Should().Be("3");
        createCandidateEvents[2].MajorityElectionCandidate.Number.Should().Be("4");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ImportService.ImportServiceClient(channel)
            .ImportMajorityElectionCandidatesAsync(await CreateValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    private async Task<ImportMajorityElectionCandidatesRequest> CreateValidRequest(bool v5 = false)
    {
        var contest = await LoadContestImport(SharedProto.ImportType.Ech157, EchTestFiles.GetTestFilePath(v5 ? EchTestFiles.Ech0157V5FileName : EchTestFiles.Ech0157FileName));

        var majorityElectionImport = contest.MajorityElections[0];

        return new ImportMajorityElectionCandidatesRequest
        {
            MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
            Candidates = { majorityElectionImport.Candidates },
        };
    }
}
