// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.EventProcessors;

public class MajorityElectionBallotGroupProcessor :
    IEventProcessor<MajorityElectionBallotGroupCreated>,
    IEventProcessor<MajorityElectionBallotGroupUpdated>,
    IEventProcessor<MajorityElectionBallotGroupDeleted>,
    IEventProcessor<MajorityElectionBallotGroupCandidatesUpdated>
{
    private readonly IDbRepository<DataContext, MajorityElectionBallotGroup> _repo;
    private readonly IDbRepository<DataContext, MajorityElection> _electionRepo;
    private readonly IDbRepository<DataContext, MajorityElectionBallotGroupEntry> _entryRepo;
    private readonly IDbRepository<DataContext, MajorityElectionBallotGroupEntryCandidate> _entryCandidateRepo;
    private readonly IMapper _mapper;
    private readonly EventLoggerAdapter _eventLogger;

    public MajorityElectionBallotGroupProcessor(
        IDbRepository<DataContext, MajorityElectionBallotGroup> repo,
        IDbRepository<DataContext, MajorityElectionBallotGroupEntry> entryRepo,
        IDbRepository<DataContext, MajorityElectionBallotGroupEntryCandidate> entryCandidateRepo,
        IDbRepository<DataContext, MajorityElection> electionRepo,
        IMapper mapper,
        EventLoggerAdapter eventLogger)
    {
        _repo = repo;
        _entryRepo = entryRepo;
        _entryCandidateRepo = entryCandidateRepo;
        _mapper = mapper;
        _eventLogger = eventLogger;
        _electionRepo = electionRepo;
    }

    public async Task Process(MajorityElectionBallotGroupCreated eventData)
    {
        var model = _mapper.Map<MajorityElectionBallotGroup>(eventData.BallotGroup);

        var nrOfMandates = await _electionRepo.Query()
            .Include(x => x.SecondaryMajorityElections)
            .Select(x => new
            {
                PrimaryNrOfMandates = new { x.Id, x.NumberOfMandates },
                SecondaryNrOfMandates = x.SecondaryMajorityElections.Select(y => new { y.Id, y.NumberOfMandates }).ToList(),
            })
            .FirstOrDefaultAsync(x => x.PrimaryNrOfMandates.Id == model.MajorityElectionId)
            ?? throw new EntityNotFoundException(model.MajorityElectionId);
        var nrOfMandatesByElectionId = nrOfMandates.SecondaryNrOfMandates.ToDictionary(x => x.Id, x => x.NumberOfMandates);
        nrOfMandatesByElectionId[nrOfMandates.PrimaryNrOfMandates.Id] = nrOfMandates.PrimaryNrOfMandates.NumberOfMandates;

        foreach (var entry in model.Entries)
        {
            entry.UpdateCandidateCountOk(nrOfMandatesByElectionId[entry.PrimaryMajorityElectionId ?? entry.SecondaryMajorityElectionId!.Value]);
        }

        await _repo.Create(model);
        await _eventLogger.LogMajorityElectionBallotGroupEvent(eventData, await GetMajorityElectionBallotGroup(model.Id));
    }

    public async Task Process(MajorityElectionBallotGroupUpdated eventData)
    {
        var model = _mapper.Map<MajorityElectionBallotGroup>(eventData.BallotGroup);
        var existingBallotGroup = await _repo.Query()
            .AsTracking() // tracking needed to resolve union entries correctly (cycle)
            .AsSplitQuery()
            .Include(bg => bg.MajorityElection.ElectionGroup!.SecondaryMajorityElections)
            .Include(bg => bg.Entries)
            .ThenInclude(x => x.PrimaryMajorityElection)
            .Include(x => x.Entries)
            .FirstOrDefaultAsync(bg => bg.Id == model.Id)
            ?? throw new EntityNotFoundException(model.Id);

        existingBallotGroup.Description = model.Description;
        existingBallotGroup.ShortDescription = model.ShortDescription;

        var existingEntriesById = existingBallotGroup.Entries.ToDictionary(x => x.Id);
        var nrOfMandatesByElectionId = existingBallotGroup.MajorityElection.ElectionGroup?.SecondaryMajorityElections
            .ToDictionary(x => x.Id, x => x.NumberOfMandates)
            ?? new Dictionary<Guid, int>();
        nrOfMandatesByElectionId[existingBallotGroup.MajorityElection.Id] = existingBallotGroup.MajorityElection.NumberOfMandates;
        foreach (var entry in model.Entries)
        {
            if (!existingEntriesById.TryGetValue(entry.Id, out var existingEntry))
            {
                entry.BallotGroupId = existingBallotGroup.Id;
                await _entryRepo.Create(entry);
                existingBallotGroup.Entries.Add(entry);
                entry.UpdateCandidateCountOk(nrOfMandatesByElectionId[entry.PrimaryMajorityElectionId ?? entry.SecondaryMajorityElectionId ?? Guid.Empty]);
            }
            else
            {
                // When the blank row count is used (old version) this field is set in the BallotGroupUpdated event instead of the BallotGroupCandidatesUpdated event.
                if (!eventData.BallotGroup.BlankRowCountUnused)
                {
                    existingEntry.BlankRowCount = entry.BlankRowCount;
                }

                existingEntry.UpdateCandidateCountOk();
            }
        }

        await _repo.Update(existingBallotGroup);
        model.MajorityElection = existingBallotGroup.MajorityElection;
        await _eventLogger.LogMajorityElectionBallotGroupEvent(eventData, model);
    }

    public async Task Process(MajorityElectionBallotGroupDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.BallotGroupId);
        var existingBallotGroup = await GetMajorityElectionBallotGroup(id);

        await _repo.DeleteByKey(id);

        var ballotGroupsToUpdate = await _repo.Query()
            .Where(bg => bg.MajorityElectionId == existingBallotGroup.MajorityElectionId
                && bg.Position > existingBallotGroup.Position)
            .ToListAsync();

        foreach (var ballotGroup in ballotGroupsToUpdate)
        {
            ballotGroup.Position--;
        }

        await _repo.UpdateRange(ballotGroupsToUpdate);
        await _eventLogger.LogMajorityElectionBallotGroupEvent(eventData, existingBallotGroup);
    }

    public async Task Process(MajorityElectionBallotGroupCandidatesUpdated eventData)
    {
        var ballotGroupId = GuidParser.Parse(eventData.BallotGroupCandidates.BallotGroupId);
        var entryCandidates = eventData.BallotGroupCandidates.EntryCandidates.ToDictionary(
            e => GuidParser.Parse(e.BallotGroupEntryId),
            e => e.CandidateIds.Select(GuidParser.Parse).ToList());
        var individualCandidatesVoteCountByEntryId = eventData.BallotGroupCandidates.EntryCandidates.ToDictionary(
            e => GuidParser.Parse(e.BallotGroupEntryId),
            e => e.IndividualCandidatesVoteCount);
        var blankRowCountByEntryId = eventData.BallotGroupCandidates.EntryCandidates.ToDictionary(
            e => GuidParser.Parse(e.BallotGroupEntryId),
            e => e.BlankRowCount);

        var ballotGroup = await _repo.Query()
            .AsTracking()
            .AsSplitQuery()
            .Include(e => e.MajorityElection)
            .Include(e => e.Entries)
            .ThenInclude(e => e.Candidates)
            .Include(e => e.Entries)
            .ThenInclude(e => e.PrimaryMajorityElection)
            .Include(e => e.Entries)
            .ThenInclude(e => e.SecondaryMajorityElection)
            .FirstOrDefaultAsync(e => e.Id == ballotGroupId)
            ?? throw new EntityNotFoundException(ballotGroupId);
        var ballotGroupEntriesById = ballotGroup.Entries.ToDictionary(x => x.Id);

        var candidatesToCreate = new List<MajorityElectionBallotGroupEntryCandidate>();
        var ballotGroupEntriesToUpdate = new List<MajorityElectionBallotGroupEntry>();
        foreach (var (ballotGroupEntryId, candidateIds) in entryCandidates)
        {
            var ballotGroupEntry = ballotGroupEntriesById[ballotGroupEntryId];
            ballotGroupEntry.IndividualCandidatesVoteCount = individualCandidatesVoteCountByEntryId.GetValueOrDefault(ballotGroupEntry.Id, 0);
            ballotGroupEntry.CountOfCandidates = candidateIds.Count;

            if (blankRowCountByEntryId.TryGetValue(ballotGroupEntryId, out var blankRowCount) && blankRowCount.HasValue)
            {
                ballotGroupEntry.BlankRowCount = blankRowCount!.Value;
            }

            ballotGroupEntry.UpdateCandidateCountOk();

            // Delete all existing candidates
            ballotGroupEntry.Candidates.Clear();
            ballotGroupEntriesToUpdate.Add(ballotGroupEntry);

            // Add all candidates
            foreach (var candidateId in candidateIds)
            {
                var candidate = new MajorityElectionBallotGroupEntryCandidate
                {
                    BallotGroupEntry = ballotGroupEntry,
                    Id = Guid.NewGuid(),
                    PrimaryElectionCandidateId = ballotGroupEntry.PrimaryMajorityElectionId.HasValue ? candidateId : null,
                    SecondaryElectionCandidateId = ballotGroupEntry.PrimaryMajorityElectionId.HasValue ? null : candidateId,
                };
                candidatesToCreate.Add(candidate);
            }
        }

        await _entryRepo.UpdateRange(ballotGroupEntriesToUpdate);
        await _entryCandidateRepo.CreateRange(candidatesToCreate);
        await _eventLogger.LogMajorityElectionBallotGroupEvent(eventData, ballotGroup);
    }

    private async Task<MajorityElectionBallotGroup> GetMajorityElectionBallotGroup(Guid id)
    {
        return await _repo.Query()
            .Include(bg => bg.MajorityElection)
            .Include(bg => bg.Entries)
            .FirstOrDefaultAsync(bg => bg.Id == id)
            ?? throw new EntityNotFoundException(id);
    }
}
