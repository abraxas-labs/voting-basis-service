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

        await CantonAdminClient.ImportMajorityElectionCandidatesAsync(request);
        var createCandidateEvents1 = EventPublisherMock.GetPublishedEvents<MajorityElectionCandidateCreated>().ToList();
        var deleteCandidateEvents1 = EventPublisherMock.GetPublishedEvents<MajorityElectionCandidateDeleted>().ToList();

        await TestEventPublisher.Publish(deleteCandidateEvents1.ToArray());
        _eventIdCounter += deleteCandidateEvents1.Count;
        await TestEventPublisher.Publish(createCandidateEvents1.ToArray());
        _eventIdCounter += createCandidateEvents1.Count;
        EventPublisherMock.Clear();
        await CantonAdminClient.ImportMajorityElectionCandidatesAsync(request);
        var createCandidateEvents2 = EventPublisherMock.GetPublishedEvents<MajorityElectionCandidateCreated>().ToList();
        var deleteCandidateEvents2 = EventPublisherMock.GetPublishedEvents<MajorityElectionCandidateDeleted>().ToList();

        await TestEventPublisher.Publish(_eventIdCounter, deleteCandidateEvents2.ToArray());
        _eventIdCounter += deleteCandidateEvents2.Count;
        await TestEventPublisher.Publish(_eventIdCounter, createCandidateEvents2.ToArray());

        createCandidateEvents1.Should().HaveCount(3);
        deleteCandidateEvents1.Should().HaveCount(1);
        createCandidateEvents2.Should().HaveCount(3);
        deleteCandidateEvents2.Should().HaveCount(3);

        var candidates = await RunOnDb(db => db.MajorityElectionCandidates
            .Where(c => c.MajorityElectionId == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen))
            .OrderBy(c => c.Position)
            .ToListAsync());
        candidates.MatchSnapshot("candidates", c => c.Id);
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

    private async Task<ImportMajorityElectionCandidatesRequest> CreateValidRequest()
    {
        var contest = await LoadContestImport(SharedProto.ImportType.Ech157, EchTestFiles.GetTestFilePath(EchTestFiles.Ech0157FileName));

        var majorityElectionImport = contest.MajorityElections[0];

        return new ImportMajorityElectionCandidatesRequest
        {
            MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
            Candidates = { majorityElectionImport.Candidates },
        };
    }
}
