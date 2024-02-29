// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Shared.V1;
using AutoMapper;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.EventSignature;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Core.Services.Read;
using Voting.Basis.Core.Services.Write;
using Voting.Basis.Ech.Converters;
using Voting.Lib.Common;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.MalwareScanner.Services;

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

    public ContestImport DeserializeImport(ImportType importType, string content, CancellationToken ct)
    {
        _malwareScannerService.EnsureFileIsClean(content, ct);

        return importType switch
        {
            ImportType.Ech157 => DeserializeEch157(content),
            ImportType.Ech159 => DeserializeEch159(content),
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

        foreach (var majorityElection in majorityElections)
        {
            majorityElection.Election.ContestId = contestId;
            var aggregate = await CreateMajorityElectionAggregate(majorityElection);
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
            var aggregate = await CreateProportionalElectionAggregate(proportionalElection);
            result.ProportionalElectionAggregates.Add(aggregate);
            politicalBusinessDomainOfInfluenceIds.Add(proportionalElection.Election.DomainOfInfluenceId);
        }

        foreach (var vote in votes)
        {
            vote.Vote.ContestId = contestId;
            var aggregate = CreateVoteAggregate(vote);
            result.VoteAggregates.Add(aggregate);
            politicalBusinessDomainOfInfluenceIds.Add(vote.Vote.DomainOfInfluenceId);
        }

        await _permissionService.EnsureDomainOfInfluencesAreChildrenOrSelf(
            contestDomainOfInfluenceId,
            politicalBusinessDomainOfInfluenceIds.ToArray());

        await _permissionService.EnsureIsOwnerOfDomainOfInfluences(politicalBusinessDomainOfInfluenceIds);

        return result;
    }

    private async Task Import(AggregateImport aggregateImport)
    {
        foreach (var majorityElection in aggregateImport.MajorityElectionAggregates)
        {
            await _aggregateRepository.Save(majorityElection);
        }

        foreach (var proportionalElection in aggregateImport.ProportionalElectionAggregates)
        {
            await _aggregateRepository.Save(proportionalElection);
        }

        foreach (var vote in aggregateImport.VoteAggregates)
        {
            await _aggregateRepository.Save(vote);
        }
    }

    private async Task<MajorityElectionAggregate> CreateMajorityElectionAggregate(MajorityElectionImport electionImport)
    {
        var majorityElection = _aggregateFactory.New<MajorityElectionAggregate>();
        majorityElection.CreateFrom(electionImport.Election);

        var electionId = electionImport.Election.Id;
        var doi = await _domainOfInfluenceReader.Get(majorityElection.DomainOfInfluenceId);

        foreach (var candidate in electionImport.Candidates)
        {
            candidate.MajorityElectionId = electionId;
            majorityElection.CreateCandidateFrom(candidate, doi.Type);
        }

        return majorityElection;
    }

    private async Task<ProportionalElectionAggregate> CreateProportionalElectionAggregate(ProportionalElectionImport electionImport)
    {
        var proportionalElection = _aggregateFactory.New<ProportionalElectionAggregate>();
        proportionalElection.CreateFrom(electionImport.Election);

        var electionId = electionImport.Election.Id;
        var doi = await _domainOfInfluenceReader.Get(proportionalElection.DomainOfInfluenceId);

        foreach (var list in electionImport.Lists)
        {
            list.List.ProportionalElectionId = electionId;
            proportionalElection.CreateListFrom(list.List);

            var listId = list.List.Id;

            foreach (var candidate in list.Candidates)
            {
                candidate.ProportionalElectionListId = listId;
                proportionalElection.CreateCandidateFrom(candidate, doi.Type);
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

            var entries = new ProportionalElectionListUnionEntries
            {
                ProportionalElectionListUnionId = listUnionProto.Id,
            };
            entries.ProportionalElectionListIds.AddRange(listUnion.ProportionalElectionListIds);
            proportionalElection.UpdateListUnionEntriesFrom(entries);
        }

        return proportionalElection;
    }

    private VoteAggregate CreateVoteAggregate(VoteImport voteImport)
    {
        var enforceResultEntryForCountingCircles = voteImport.Vote.EnforceResultEntryForCountingCircles;
        var vote = _aggregateFactory.New<VoteAggregate>();

        // since there are no ballots yet, only enforced final result entry is allowed,
        // to match the specification the vote is updated after the creation of the ballots
        // with an according EnforceResultEntryForCountingCircles value.
        voteImport.Vote.EnforceResultEntryForCountingCircles = true;
        vote.CreateFrom(voteImport.Vote);

        var voteId = voteImport.Vote.Id;

        foreach (var ballot in voteImport.Vote.Ballots)
        {
            ballot.VoteId = voteId;
            vote.CreateBallot(ballot);
        }

        if (!enforceResultEntryForCountingCircles)
        {
            voteImport.Vote.EnforceResultEntryForCountingCircles = false;
            vote.UpdateFrom(voteImport.Vote);
        }

        return vote;
    }

    private ContestImport DeserializeEch157(string content)
    {
        var contest = _ech0157Deserializer.DeserializeXml(content);
        return _mapper.Map<ContestImport>(contest);
    }

    private ContestImport DeserializeEch159(string content)
    {
        var contest = _ech0159Deserializer.DeserializeXml(content);
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
