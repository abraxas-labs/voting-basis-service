﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Shared.V1;
using AutoMapper;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.EventSignature;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Models;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Core.Services.Read;
using Voting.Basis.Core.Services.Write;
using Voting.Basis.Ech.Converters;
using Voting.Lib.Common;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.MalwareScanner.Services;
using VoteResultEntry = Voting.Basis.Data.Models.VoteResultEntry;

namespace Voting.Basis.Core.Import;

public class ImportService
{
    private readonly IMapper _mapper;
    private readonly PermissionService _permissionService;
    private readonly ContestReader _contestReader;
    private readonly ContestWriter _contestWriter;
    private readonly IAggregateFactory _aggregateFactory;
    private readonly IAggregateRepository _aggregateRepository;
    private readonly ProportionalElectionWriter _proportionalElectionWriter;
    private readonly EventSignatureService _eventSignatureService;
    private readonly IClock _clock;
    private readonly DomainOfInfluenceReader _domainOfInfluenceReader;
    private readonly IMalwareScannerService _malwareScannerService;
    private readonly Ech0157Deserializer _ech0157Deserializer;
    private readonly Ech0159Deserializer _ech0159Deserializer;

    public ImportService(
        IMapper mapper,
        PermissionService permissionService,
        ContestReader contestReader,
        ContestWriter contestWriter,
        IAggregateFactory aggregateFactory,
        IAggregateRepository aggregateRepository,
        ProportionalElectionWriter proportionalElectionWriter,
        EventSignatureService eventSignatureService,
        IClock clock,
        DomainOfInfluenceReader domainOfInfluenceReader,
        IMalwareScannerService malwareScannerService,
        Ech0157Deserializer ech0157Deserializer,
        Ech0159Deserializer ech0159Deserializer)
    {
        _mapper = mapper;
        _permissionService = permissionService;
        _contestReader = contestReader;
        _contestWriter = contestWriter;
        _aggregateFactory = aggregateFactory;
        _aggregateRepository = aggregateRepository;
        _proportionalElectionWriter = proportionalElectionWriter;
        _eventSignatureService = eventSignatureService;
        _clock = clock;
        _domainOfInfluenceReader = domainOfInfluenceReader;
        _malwareScannerService = malwareScannerService;
        _ech0157Deserializer = ech0157Deserializer;
        _ech0159Deserializer = ech0159Deserializer;
    }

    public async Task Import(ContestImport contestImport)
    {
        contestImport.Contest.Id = Guid.NewGuid();
        await EnsureActiveEventSignature(contestImport.Contest.Id);

        var aggregateImport = await CreateValidatedAggregates(
            contestImport.Contest.Id,
            contestImport.Contest.DomainOfInfluenceId,
            contestImport.MajorityElections,
            contestImport.ProportionalElections,
            contestImport.Votes);

        // Note: No need to validate the contest, that will be taken care of in the writer. If something isn't valid, nothing is imported yet -> all good.
        await _contestWriter.Create(contestImport.Contest);
        await _contestWriter.StartContestImport(contestImport.Contest.Id);
        await Import(aggregateImport);
    }

    public async Task Import(
        Guid contestId,
        IEnumerable<MajorityElectionImport> majorityElections,
        IEnumerable<ProportionalElectionImport> proportionalElections,
        IEnumerable<VoteImport> votes)
    {
        var contest = await _contestReader.Get(contestId);
        if (contest.TestingPhaseEnded)
        {
            throw new ContestTestingPhaseEndedException();
        }

        await EnsureActiveEventSignature(contestId);
        var aggregateImport = await CreateValidatedAggregates(contestId, contest.DomainOfInfluenceId, majorityElections, proportionalElections, votes);
        await _contestWriter.StartPoliticalBusinessImport(contest.Id);
        await Import(aggregateImport);
    }

    public ContestImport DeserializeImport(ImportType importType, Stream stream, CancellationToken ct)
    {
        _malwareScannerService.EnsureFileIsClean(stream, ct);
        stream.Seek(0, SeekOrigin.Begin);

        return importType switch
        {
            ImportType.Ech157 => DeserializeEch157(stream),
            ImportType.Ech159 => DeserializeEch159(stream),
            _ => throw new InvalidOperationException($"Import type {importType} is not yet supported"),
        };
    }

    private async Task<AggregateImport> CreateValidatedAggregates(
        Guid contestId,
        Guid contestDomainOfInfluenceId,
        IEnumerable<MajorityElectionImport> majorityElections,
        IEnumerable<ProportionalElectionImport> proportionalElections,
        IEnumerable<VoteImport> votes)
    {
        var politicalBusinessDomainOfInfluenceIds = new HashSet<Guid>();
        var result = new AggregateImport();
        var idVerifier = new IdVerifier();

        foreach (var majorityElection in majorityElections)
        {
            majorityElection.Election.ContestId = contestId;
            var aggregate = await CreateMajorityElectionAggregate(majorityElection, idVerifier);
            result.MajorityElectionAggregates.Add(aggregate);
            politicalBusinessDomainOfInfluenceIds.Add(majorityElection.Election.DomainOfInfluenceId);
        }

        foreach (var proportionalElection in proportionalElections)
        {
            // only mandate algorithms from canton settings allowed
            await _proportionalElectionWriter.EnsureValidProportionalElectionMandateAlgorithm(
                proportionalElection.Election.MandateAlgorithm,
                proportionalElection.Election.DomainOfInfluenceId);

            proportionalElection.Election.ContestId = contestId;
            var aggregate = await CreateProportionalElectionAggregate(proportionalElection, idVerifier);
            result.ProportionalElectionAggregates.Add(aggregate);
            politicalBusinessDomainOfInfluenceIds.Add(proportionalElection.Election.DomainOfInfluenceId);
        }

        foreach (var vote in votes)
        {
            vote.Vote.ContestId = contestId;
            var aggregate = CreateVoteAggregate(vote, idVerifier);
            result.VoteAggregates.Add(aggregate);
            politicalBusinessDomainOfInfluenceIds.Add(vote.Vote.DomainOfInfluenceId);
        }

        await _permissionService.EnsureDomainOfInfluencesAreChildrenOrSelf(
            contestDomainOfInfluenceId,
            politicalBusinessDomainOfInfluenceIds.ToArray());

        await _permissionService.EnsureIsOwnerOfDomainOfInfluencesOrHasAdminPermissions(politicalBusinessDomainOfInfluenceIds, false);

        return result;
    }

    private async Task Import(AggregateImport aggregateImport)
    {
        foreach (var majorityElection in aggregateImport.MajorityElectionAggregates)
        {
            await _aggregateRepository.SaveChunked(majorityElection);
        }

        foreach (var proportionalElection in aggregateImport.ProportionalElectionAggregates)
        {
            await _aggregateRepository.SaveChunked(proportionalElection);
        }

        foreach (var vote in aggregateImport.VoteAggregates)
        {
            await _aggregateRepository.SaveChunked(vote);
        }
    }

    private async Task<MajorityElectionAggregate> CreateMajorityElectionAggregate(MajorityElectionImport electionImport, IdVerifier idVerifier)
    {
        var majorityElection = _aggregateFactory.New<MajorityElectionAggregate>();
        majorityElection.CreateFrom(electionImport.Election);

        var electionId = electionImport.Election.Id;
        idVerifier.EnsureUnique(electionId);
        var doi = await _domainOfInfluenceReader.Get(majorityElection.DomainOfInfluenceId);
        var candidateValidationParams = new CandidateValidationParams(doi, true);

        foreach (var candidate in electionImport.Candidates)
        {
            candidate.MajorityElectionId = electionId;
            majorityElection.CreateCandidateFrom(candidate, candidateValidationParams);
            idVerifier.EnsureUnique(candidate.Id);
        }

        return majorityElection;
    }

    private async Task<ProportionalElectionAggregate> CreateProportionalElectionAggregate(ProportionalElectionImport electionImport, IdVerifier idVerifier)
    {
        var proportionalElection = _aggregateFactory.New<ProportionalElectionAggregate>();
        proportionalElection.CreateFrom(electionImport.Election);

        var electionId = electionImport.Election.Id;
        idVerifier.EnsureUnique(electionId);
        var doi = await _domainOfInfluenceReader.Get(proportionalElection.DomainOfInfluenceId);
        var candidateValidationParams = new CandidateValidationParams(doi);

        foreach (var list in electionImport.Lists)
        {
            list.List.ProportionalElectionId = electionId;
            proportionalElection.CreateListFrom(list.List);

            var listId = list.List.Id;
            idVerifier.EnsureUnique(listId);

            foreach (var candidate in list.Candidates)
            {
                candidate.ProportionalElectionListId = listId;
                proportionalElection.CreateCandidateFrom(candidate, candidateValidationParams);
                idVerifier.EnsureUnique(candidate.Id);
            }
        }

        // first import all root list unions
        var sortedListUnions = electionImport.ListUnions.OrderBy(x => x.ProportionalElectionRootListUnionId.HasValue);

        foreach (var listUnion in sortedListUnions)
        {
            var listUnionProto = new ProportionalElectionListUnion
            {
                Id = listUnion.Id,
                Description = listUnion.Description,
                Position = listUnion.Position,
                ProportionalElectionId = electionId,
                ProportionalElectionRootListUnionId = listUnion.ProportionalElectionRootListUnionId,
            };
            proportionalElection.CreateListUnionFrom(listUnionProto);
            idVerifier.EnsureUnique(listUnionProto.Id);

            var entries = new ProportionalElectionListUnionEntries
            {
                ProportionalElectionListUnionId = listUnionProto.Id,
            };
            entries.ProportionalElectionListIds.AddRange(listUnion.ProportionalElectionListIds);
            proportionalElection.UpdateListUnionEntriesFrom(entries);
        }

        return proportionalElection;
    }

    private VoteAggregate CreateVoteAggregate(VoteImport voteImport, IdVerifier idVerifier)
    {
        var enforceResultEntryForCountingCircles = voteImport.Vote.EnforceResultEntryForCountingCircles;
        var resultEntry = voteImport.Vote.ResultEntry;
        var vote = _aggregateFactory.New<VoteAggregate>();

        // since there are no ballots yet, only enforced final result entry is allowed,
        // to match the specification the vote is updated after the creation of the ballots
        // with an according EnforceResultEntryForCountingCircles value.
        voteImport.Vote.EnforceResultEntryForCountingCircles = true;
        voteImport.Vote.ResultEntry = VoteResultEntry.FinalResults;
        vote.CreateFrom(voteImport.Vote);

        var voteId = voteImport.Vote.Id;
        idVerifier.EnsureUnique(voteId);

        foreach (var ballot in voteImport.Vote.Ballots)
        {
            ballot.VoteId = voteId;
            vote.CreateBallot(ballot);
            idVerifier.EnsureUnique(ballot.Id);
        }

        var needsUpdate = false;
        if (!enforceResultEntryForCountingCircles)
        {
            voteImport.Vote.EnforceResultEntryForCountingCircles = false;
            needsUpdate = true;
        }

        if (resultEntry != VoteResultEntry.FinalResults)
        {
            voteImport.Vote.ResultEntry = resultEntry;
            needsUpdate = true;
        }

        if (needsUpdate)
        {
            vote.UpdateFrom(voteImport.Vote);
        }

        return vote;
    }

    private ContestImport DeserializeEch157(Stream stream)
    {
        var contest = _ech0157Deserializer.DeserializeXml(stream);
        return _mapper.Map<ContestImport>(contest);
    }

    private ContestImport DeserializeEch159(Stream stream)
    {
        var contest = _ech0159Deserializer.DeserializeXml(stream);
        return _mapper.Map<ContestImport>(contest);
    }

    private async Task EnsureActiveEventSignature(Guid contestId)
    {
        // In some cases, ensuring an active event signature must be done early.
        // Usually, the event signature is started with ValidFrom equal to the first saved event.
        // However, in some cases the workflow looks like this:
        // 1. Create uncommitted events on aggregate A1 (timestamps of events will be set here, on event creation).
        // 2. Create uncommitted events on aggregate A2.
        // 3. Save aggregate A2.
        // 4. Save aggregate A1.
        // If there is no active event signature in step 3, a new event signature will be created.
        // The ValidFrom value of the event signature will be equal to the event timestamp of the first uncommitted event of A2.
        // Saving A1 will now lead to an exception because the timestamps of the A1 events are earlier than the ValidFrom of the signature.
        await _eventSignatureService.EnsureActiveSignature(contestId, _clock.UtcNow);
    }
}
