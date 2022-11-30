// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Shared.V1;
using AutoMapper;
using eCH_0157_4_0;
using eCH_0159_4_0;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Core.Services.Read;
using Voting.Basis.Core.Services.Write;
using Voting.Basis.Ech.Converters;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Store;

namespace Voting.Basis.Core.Import;

public class ImportService
{
    private readonly IMapper _mapper;
    private readonly IAuth _auth;
    private readonly Ech159Deserializer _ech159Deserializer;
    private readonly Ech157Deserializer _ech157Deserializer;
    private readonly PermissionService _permissionService;
    private readonly ContestReader _contestReader;
    private readonly ContestWriter _contestWriter;
    private readonly IAggregateFactory _aggregateFactory;
    private readonly IAggregateRepository _aggregateRepository;
    private readonly ProportionalElectionWriter _proportionalElectionWriter;

    public ImportService(
        IMapper mapper,
        IAuth auth,
        Ech159Deserializer ech159Deserializer,
        Ech157Deserializer ech157Deserializer,
        PermissionService permissionService,
        ContestReader contestReader,
        ContestWriter contestWriter,
        IAggregateFactory aggregateFactory,
        IAggregateRepository aggregateRepository,
        ProportionalElectionWriter proportionalElectionWriter)
    {
        _mapper = mapper;
        _auth = auth;
        _ech159Deserializer = ech159Deserializer;
        _ech157Deserializer = ech157Deserializer;
        _permissionService = permissionService;
        _contestReader = contestReader;
        _contestWriter = contestWriter;
        _aggregateFactory = aggregateFactory;
        _aggregateRepository = aggregateRepository;
        _proportionalElectionWriter = proportionalElectionWriter;
    }

    public async Task Import(ContestImport contestImport)
    {
        _auth.EnsureAdminOrElectionAdmin();

        contestImport.Contest.Id = Guid.NewGuid();
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
        _auth.EnsureAdminOrElectionAdmin();

        var contest = await _contestReader.Get(contestId);
        if (contest.TestingPhaseEnded)
        {
            throw new ContestTestingPhaseEndedException();
        }

        var aggregateImport = await CreateValidatedAggregates(contestId, contest.DomainOfInfluenceId, majorityElections, proportionalElections, votes);
        await _contestWriter.StartPoliticalBusinessImport(contest.Id);
        await Import(aggregateImport);
    }

    public ContestImport DeserializeImport(ImportType importType, string content)
    {
        _auth.EnsureAdminOrElectionAdmin();

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
            var aggregate = CreateMajorityElectionAggregate(majorityElection);
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
            var aggregate = CreateProportionalElectionAggregate(proportionalElection);
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

    private MajorityElectionAggregate CreateMajorityElectionAggregate(MajorityElectionImport electionImport)
    {
        var majorityElection = _aggregateFactory.New<MajorityElectionAggregate>();
        majorityElection.CreateFrom(electionImport.Election);

        var electionId = electionImport.Election.Id;

        foreach (var candidate in electionImport.Candidates)
        {
            candidate.MajorityElectionId = electionId;
            majorityElection.CreateCandidateFrom(candidate);
        }

        return majorityElection;
    }

    private ProportionalElectionAggregate CreateProportionalElectionAggregate(ProportionalElectionImport electionImport)
    {
        var proportionalElection = _aggregateFactory.New<ProportionalElectionAggregate>();
        proportionalElection.CreateFrom(electionImport.Election);

        var electionId = electionImport.Election.Id;

        foreach (var list in electionImport.Lists)
        {
            list.List.ProportionalElectionId = electionId;
            proportionalElection.CreateListFrom(list.List);

            var listId = list.List.Id;

            foreach (var candidate in list.Candidates)
            {
                candidate.ProportionalElectionListId = listId;
                proportionalElection.CreateCandidateFrom(candidate);
            }
        }

        foreach (var listUnion in electionImport.ListUnions)
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
        var ech157 = EchDeserializer.FromXml<DeliveryType>(content);
        var contest = _ech157Deserializer.FromEventInitialDelivery(ech157);
        return _mapper.Map<ContestImport>(contest);
    }

    private ContestImport DeserializeEch159(string content)
    {
        var ech159 = EchDeserializer.FromXml<Delivery>(content);
        var contest = _ech159Deserializer.FromEventInitialDelivery(ech159);
        return _mapper.Map<ContestImport>(contest);
    }
}
