﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Metadata;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Basis.Core.Auth;
using Voting.Basis.Test.ImportTests.TestFiles;
using Voting.Basis.Test.MockedData;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.ImportTests;

public class ImportContestTest : BaseImportTest
{
    public ImportContestTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldWork()
    {
        var contest = await CreateValidContestImport();
        await CantonAdminClient.ImportContestAsync(new ImportContestRequest { Contest = contest });

        var peListCreatedEventsWithMetadata = EventPublisherMock.GetPublishedEvents<ProportionalElectionListCreated, EventSignatureBusinessMetadata>();
        var ballotCreatedEventsWithMetadata = EventPublisherMock.GetPublishedEvents<BallotCreated, EventSignatureBusinessMetadata>();
        var meCandidateCreatedEventsWithMetadata = EventPublisherMock.GetPublishedEvents<MajorityElectionCandidateCreated, EventSignatureBusinessMetadata>();

        var childEventsMetadata = peListCreatedEventsWithMetadata
            .Select(e => e.Metadata)
            .Concat(ballotCreatedEventsWithMetadata.Select(e => e.Metadata))
            .Concat(meCandidateCreatedEventsWithMetadata.Select(e => e.Metadata))
            .ToList();

        childEventsMetadata.Where(m => m == null).Should().HaveCount(0);
        childEventsMetadata.Should().HaveCountGreaterThan(0);
        childEventsMetadata.DistinctBy(m => m!.ContestId).Should().HaveCount(1);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvents()
    {
        var contest = await CreateValidContestImport();
        await CantonAdminClient.ImportContestAsync(new ImportContestRequest { Contest = contest });

        var peListCreatedEventsWithMetadata = EventPublisherMock.GetPublishedEventsWithMetadata<ProportionalElectionListCreated>();
        var ballotCreatedEventsWithMetadata = EventPublisherMock.GetPublishedEventsWithMetadata<BallotCreated>();
        var meCandidateCreatedEventsWithMetadata = EventPublisherMock.GetPublishedEventsWithMetadata<MajorityElectionCandidateCreated>();

        var childEvents = peListCreatedEventsWithMetadata
            .Concat(ballotCreatedEventsWithMetadata)
            .Concat(meCandidateCreatedEventsWithMetadata)
            .ToList();

        childEvents.Where(m => m == null).Should().HaveCount(0);
        childEvents.Should().HaveCountGreaterThan(0);

        var contestId = (childEvents.First().Metadata as EventSignatureBusinessMetadata)!.ContestId;

        foreach (var childEventMetadata in childEvents)
        {
            EnsureIsSignedBusinessEvent(childEventMetadata, contestId);
        }

        // ensure that a public key signed event got emitted.
        var publicKeyCreatedEvent = EventPublisherMock.GetSinglePublishedEvent<EventSignaturePublicKeyCreated>();
        publicKeyCreatedEvent.ContestId.Should().Be(contestId);
    }

    [Fact]
    public async Task TestWithDuplicatedCandidateIdShouldThrow()
    {
        var contest = await CreateValidContestImport();
        contest.MajorityElections[0].Candidates[0].Id = contest.MajorityElections[0].Candidates[1].Id;
        await AssertStatus(
            async () => await CantonAdminClient.ImportContestAsync(new ImportContestRequest { Contest = contest }),
            StatusCode.InvalidArgument,
            "This id is not unique");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        var contest = await CreateValidContestImport();
        await new ImportService.ImportServiceClient(channel)
            .ImportContestAsync(new ImportContestRequest
            {
                Contest = contest,
            });
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    private async Task<ProtoModels.ContestImport> CreateValidContestImport()
    {
        var contest1 = await LoadContestImport(SharedProto.ImportType.Ech157, EchTestFiles.GetTestFilePath(EchTestFiles.Ech0157FileName));
        var contest2 = await LoadContestImport(SharedProto.ImportType.Ech159, EchTestFiles.GetTestFilePath(EchTestFiles.Ech0159FileName));

        contest1.MajorityElections.AddRange(contest2.MajorityElections);
        contest1.ProportionalElections.AddRange(contest2.ProportionalElections);
        contest1.Votes.AddRange(contest2.Votes);

        foreach (var majorityElection in contest1.MajorityElections)
        {
            majorityElection.Election.PoliticalBusinessNumber = "11new";
            majorityElection.Election.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau;
        }

        foreach (var proportionalElection in contest1.ProportionalElections)
        {
            proportionalElection.Election.PoliticalBusinessNumber = "11new";
            proportionalElection.Election.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau;
        }

        foreach (var vote in contest1.Votes)
        {
            vote.Vote.PoliticalBusinessNumber = "11new";
            vote.Vote.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau;
        }

        contest1.Contest.EndOfTestingPhase = new DateTime(2022, 5, 16, 14, 40, 12, DateTimeKind.Utc).ToTimestamp();
        contest1.Contest.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau;

        return contest1;
    }
}
