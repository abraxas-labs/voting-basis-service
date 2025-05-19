// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Extensions;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;
using Voting.Basis.Ech.Utils;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.EventProcessors;

public class ProportionalElectionProcessor :
    IEventProcessor<ProportionalElectionCreated>,
    IEventProcessor<ProportionalElectionUpdated>,
    IEventProcessor<ProportionalElectionAfterTestingPhaseUpdated>,
    IEventProcessor<ProportionalElectionActiveStateUpdated>,
    IEventProcessor<ProportionalElectionDeleted>,
    IEventProcessor<ProportionalElectionToNewContestMoved>,
    IEventProcessor<ProportionalElectionListCreated>,
    IEventProcessor<ProportionalElectionListUpdated>,
    IEventProcessor<ProportionalElectionListAfterTestingPhaseUpdated>,
    IEventProcessor<ProportionalElectionListsReordered>,
    IEventProcessor<ProportionalElectionListDeleted>,
    IEventProcessor<ProportionalElectionListUnionCreated>,
    IEventProcessor<ProportionalElectionListUnionUpdated>,
    IEventProcessor<ProportionalElectionListUnionDeleted>,
    IEventProcessor<ProportionalElectionListUnionsReordered>,
    IEventProcessor<ProportionalElectionListUnionEntriesUpdated>,
    IEventProcessor<ProportionalElectionListUnionMainListUpdated>,
    IEventProcessor<ProportionalElectionCandidateCreated>,
    IEventProcessor<ProportionalElectionCandidateUpdated>,
    IEventProcessor<ProportionalElectionCandidateAfterTestingPhaseUpdated>,
    IEventProcessor<ProportionalElectionCandidatesReordered>,
    IEventProcessor<ProportionalElectionCandidateDeleted>,
    IEventProcessor<ProportionalElectionMandateAlgorithmUpdated>
{
    private readonly IDbRepository<DataContext, ProportionalElection> _repo;
    private readonly SimplePoliticalBusinessBuilder<ProportionalElection> _simplePoliticalBusinessBuilder;
    private readonly ProportionalElectionListRepo _listRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionListUnion> _listUnionRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionCandidate> _candidateRepo;
    private readonly IMapper _mapper;
    private readonly ProportionalElectionListUnionEntryRepo _proportionalElectionListUnionEntryRepo;
    private readonly ProportionalElectionUnionListBuilder _unionListBuilder;
    private readonly ProportionalElectionListBuilder _listBuilder;
    private readonly EventLoggerAdapter _eventLogger;
    private readonly ILogger<ProportionalElectionProcessor> _logger;

    public ProportionalElectionProcessor(
        ILogger<ProportionalElectionProcessor> logger,
        IMapper mapper,
        IDbRepository<DataContext, ProportionalElection> repo,
        ProportionalElectionListRepo listRepo,
        IDbRepository<DataContext, ProportionalElectionCandidate> candidateRepo,
        ProportionalElectionListUnionEntryRepo proportionalElectionListUnionEntryRepo,
        IDbRepository<DataContext, ProportionalElectionListUnion> listUnionRepo,
        ProportionalElectionListBuilder listBuilder,
        ProportionalElectionUnionListBuilder unionListBuilder,
        EventLoggerAdapter eventLogger,
        SimplePoliticalBusinessBuilder<ProportionalElection> simplePoliticalBusinessBuilder)
    {
        _logger = logger;
        _repo = repo;
        _listRepo = listRepo;
        _candidateRepo = candidateRepo;
        _mapper = mapper;
        _proportionalElectionListUnionEntryRepo = proportionalElectionListUnionEntryRepo;
        _listUnionRepo = listUnionRepo;
        _unionListBuilder = unionListBuilder;
        _eventLogger = eventLogger;
        _simplePoliticalBusinessBuilder = simplePoliticalBusinessBuilder;
        _listBuilder = listBuilder;
    }

    public async Task Process(ProportionalElectionCreated eventData)
    {
        var model = _mapper.Map<ProportionalElection>(eventData.ProportionalElection);

        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (model.ReviewProcedure == ProportionalElectionReviewProcedure.Unspecified)
        {
            model.ReviewProcedure = ProportionalElectionReviewProcedure.Electronically;
        }

        await _repo.Create(model);
        await _simplePoliticalBusinessBuilder.Create(model);
        await _eventLogger.LogProportionalElectionEvent(eventData, model);
    }

    public async Task Process(ProportionalElectionUpdated eventData)
    {
        var model = _mapper.Map<ProportionalElection>(eventData.ProportionalElection);

        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (model.ReviewProcedure == ProportionalElectionReviewProcedure.Unspecified)
        {
            model.ReviewProcedure = ProportionalElectionReviewProcedure.Electronically;
        }

        var existingModel = await _repo.GetByKey(model.Id)
            ?? throw new EntityNotFoundException(model.Id);

        await _repo.Update(model);
        await _simplePoliticalBusinessBuilder.Update(model);

        if (model.NumberOfMandates != existingModel.NumberOfMandates)
        {
            await _listRepo.UpdateCandidateCountOk(model.Id, model.NumberOfMandates);
        }

        await _eventLogger.LogProportionalElectionEvent(eventData, model);
    }

    public async Task Process(ProportionalElectionAfterTestingPhaseUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.Id);
        var election = await _repo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);

        _mapper.Map(eventData, election);
        await _repo.Update(election);
        await _simplePoliticalBusinessBuilder.Update(election);

        await _eventLogger.LogProportionalElectionEvent(eventData, election);
    }

    public async Task Process(ProportionalElectionDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.ProportionalElectionId);
        try
        {
            var existingModel = await GetElection(id);
            await _repo.DeleteByKey(id);
            await _simplePoliticalBusinessBuilder.Delete(existingModel);
            await _unionListBuilder.RemoveListsWithNoEntries();
            await _eventLogger.LogProportionalElectionEvent(eventData, existingModel);
        }
        catch (EntityNotFoundException)
        {
            // skip event processing to prevent race condition if proportional election was deleted from other process.
            _logger.LogWarning("event 'ProportionalElectionDeleted' skipped. proportional election {id} has already been deleted", id);
        }
    }

    public async Task Process(ProportionalElectionToNewContestMoved eventData)
    {
        var id = GuidParser.Parse(eventData.ProportionalElectionId);
        var existingModel = await GetElection(id);

        existingModel.ContestId = GuidParser.Parse(eventData.NewContestId);
        await _repo.Update(existingModel);
        await _simplePoliticalBusinessBuilder.Update(existingModel);
        await _eventLogger.LogProportionalElectionEvent(eventData, existingModel);
    }

    public async Task Process(ProportionalElectionActiveStateUpdated eventData)
    {
        var electionId = GuidParser.Parse(eventData.ProportionalElectionId);
        var existingModel = await GetElection(electionId);

        existingModel.Active = eventData.Active;
        await _repo.Update(existingModel);
        await _simplePoliticalBusinessBuilder.Update(existingModel);
        await _eventLogger.LogProportionalElectionEvent(eventData, existingModel);
    }

    public async Task Process(ProportionalElectionListCreated eventData)
    {
        var model = _mapper.Map<ProportionalElectionList>(eventData.ProportionalElectionList);
        await _listRepo.Create(model);
        await _unionListBuilder.RebuildForProportionalElection(model.ProportionalElectionId);
        var proportionalElection = await GetElection(model.ProportionalElectionId);
        await _eventLogger.LogProportionalElectionListEvent(eventData, model, proportionalElection.ContestId, proportionalElection.DomainOfInfluenceId);
    }

    public async Task Process(ProportionalElectionListUpdated eventData)
    {
        var model = _mapper.Map<ProportionalElectionList>(eventData.ProportionalElectionList);
        var existingModel = await GetList(model.Id);
        model.CountOfCandidates = existingModel.CountOfCandidates;
        model.ListUnionDescription = existingModel.ListUnionDescription;
        model.SubListUnionDescription = existingModel.SubListUnionDescription;
        model.UpdateCandidateCountOk(existingModel.ProportionalElection.NumberOfMandates);

        await _listRepo.Update(model);

        var shortDescriptionChanged = !existingModel.ShortDescription.KeysAndValuesEqual(model.ShortDescription);
        if (shortDescriptionChanged || model.OrderNumber != existingModel.OrderNumber)
        {
            await _unionListBuilder.RebuildForProportionalElection(model.ProportionalElectionId);
        }

        if (shortDescriptionChanged)
        {
            await _listBuilder.UpdateListUnionDescriptionsReferencingList(model.Id);
        }

        await _eventLogger.LogProportionalElectionListEvent(eventData, model, existingModel.ProportionalElection.ContestId, existingModel.ProportionalElection.DomainOfInfluenceId);
    }

    public async Task Process(ProportionalElectionListAfterTestingPhaseUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.Id);
        var list = await GetList(id);

        _mapper.Map(eventData, list);
        await _listRepo.Update(list);

        if (!list.ShortDescription.KeysAndValuesEqual(eventData.ShortDescription))
        {
            await _unionListBuilder.RebuildForProportionalElection(list.ProportionalElectionId);
            await _listBuilder.UpdateListUnionDescriptionsReferencingList(id);
        }

        await _eventLogger.LogProportionalElectionListEvent(eventData, list, list.ProportionalElection.ContestId, list.ProportionalElection.DomainOfInfluenceId);
    }

    public async Task Process(ProportionalElectionListsReordered eventData)
    {
        var proportionalElectionId = GuidParser.Parse(eventData.ProportionalElectionId);
        var proportionalElection = await _repo.Query()
            .Include(p => p.ProportionalElectionLists)
            .FirstOrDefaultAsync(p => p.Id == proportionalElectionId)
            ?? throw new EntityNotFoundException(proportionalElectionId);

        var grouped = eventData.ListOrders.Orders
            .GroupBy(o => o.Id)
            .ToDictionary(x => GuidParser.Parse(x.Key), x => x.Single().Position);

        foreach (var list in proportionalElection.ProportionalElectionLists)
        {
            list.Position = grouped[list.Id];
        }

        await _listRepo.UpdateRange(proportionalElection.ProportionalElectionLists);
        await _eventLogger.LogProportionalElectionEvent(eventData, proportionalElection);
    }

    public async Task Process(ProportionalElectionListDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.ProportionalElectionListId);
        var existingList = await _listRepo.Query()
                               .AsTracking() // tracking is not really needed but required due to query cycles
                               .AsSplitQuery()
                               .Include(l => l.ProportionalElection)
                               .Include(l => l.ProportionalElectionListUnionEntries)
                               .ThenInclude(l => l.ProportionalElectionListUnion.ProportionalElectionListUnionEntries)
                               .FirstOrDefaultAsync(l => l.Id == id)
                           ?? throw new EntityNotFoundException(id);
        if (existingList is null)
        {
            // skip event processing to prevent race condition if proportional election list was deleted from other process.
            _logger.LogWarning("event 'ProportionalElectionListDeleted' skipped. proportional election list {id} has already been deleted", id);
            return;
        }

        await _listRepo.DeleteByKey(id);

        var listsToUpdate = await _listRepo.Query()
            .Where(l => l.ProportionalElectionId == existingList.ProportionalElectionId && l.Position > existingList.Position)
            .ToListAsync();
        foreach (var list in listsToUpdate)
        {
            list.Position--;
        }

        await _listRepo.UpdateRange(listsToUpdate);
        await _unionListBuilder.RemoveListsWithNoEntries();

        // touched list ids of lists which are in same list unions as the deleted list
        var touchedListIds = existingList.ProportionalElectionListUnionEntries
            .SelectMany(x => x.ProportionalElectionListUnion.ProportionalElectionListUnionEntries)
            .Select(x => x.ProportionalElectionListId)
            .ToHashSet();
        await _listBuilder.UpdateListUnionDescriptions(touchedListIds);

        await _eventLogger.LogProportionalElectionListEvent(eventData, existingList);

        // remove self referencing loop
        existingList.ProportionalElection.ProportionalElectionLists = null!;
    }

    public async Task Process(ProportionalElectionListUnionCreated eventData)
    {
        var model = _mapper.Map<ProportionalElectionListUnion>(eventData.ProportionalElectionListUnion);
        await _listUnionRepo.Create(model);
        await _eventLogger.LogProportionalElectionListUnionEvent(eventData, await GetListUnion(model.Id));
    }

    public async Task Process(ProportionalElectionListUnionUpdated eventData)
    {
        var model = _mapper.Map<ProportionalElectionListUnion>(eventData.ProportionalElectionListUnion);
        var existingListUnion = await GetListUnion(model.Id);

        existingListUnion.Description = model.Description;

        await _listUnionRepo.Update(existingListUnion);
        await _eventLogger.LogProportionalElectionListUnionEvent(eventData, model, existingListUnion.ProportionalElection.ContestId, existingListUnion.ProportionalElection.DomainOfInfluenceId);
    }

    public async Task Process(ProportionalElectionListUnionsReordered eventData)
    {
        var proportionalElectionId = GuidParser.Parse(eventData.ProportionalElectionId);
        var proportionalElectionRootListUnionId = !string.IsNullOrEmpty(eventData.ProportionalElectionRootListUnionId)
            ? Guid.Parse(eventData.ProportionalElectionRootListUnionId)
            : (Guid?)null;

        var proportionalElection = await _repo.Query()
            .Include(p => p.ProportionalElectionListUnions)
            .FirstOrDefaultAsync(p => p.Id == proportionalElectionId)
            ?? throw new EntityNotFoundException(proportionalElectionId);

        // filter can be included with includes with ef core 5
        proportionalElection.ProportionalElectionListUnions = proportionalElection.ProportionalElectionListUnions
            .Where(u => u.ProportionalElectionRootListUnionId == proportionalElectionRootListUnionId)
            .ToList();

        var grouped = eventData.ProportionalElectionListUnionOrders.Orders
            .GroupBy(o => o.Id)
            .ToDictionary(x => GuidParser.Parse(x.Key), x => x.Select(y => y.Position).OrderBy(y => y).ToList());

        foreach (var listUnion in proportionalElection.ProportionalElectionListUnions)
        {
            listUnion.Position = grouped[listUnion.Id][0];
        }

        await _listUnionRepo.UpdateRange(proportionalElection.ProportionalElectionListUnions);
        await _eventLogger.LogProportionalElectionEvent(eventData, proportionalElection);
    }

    public async Task Process(ProportionalElectionListUnionDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.ProportionalElectionListUnionId);
        var existingListUnion = await _listUnionRepo.Query()
            .Include(lu => lu.ProportionalElection)
            .Include(x => x.ProportionalElectionListUnionEntries)
            .FirstOrDefaultAsync(lu => lu.Id == id)
            ?? throw new EntityNotFoundException(id);
        if (existingListUnion is null)
        {
            // skip event processing to prevent race condition if proportional election list union was deleted from other process.
            _logger.LogWarning("event 'ProportionalElectionListUnionDeleted' skipped. proportional election list union {id} has already been deleted", id);
            return;
        }

        await _listUnionRepo.DeleteByKey(id);

        var listUnionsToUpdate = await _listUnionRepo.Query()
            .Where(l => l.ProportionalElectionId == existingListUnion.ProportionalElectionId && l.Position > existingListUnion.Position)
            .ToListAsync();

        foreach (var listUnion in listUnionsToUpdate)
        {
            listUnion.Position--;
        }

        await _listUnionRepo.UpdateRange(listUnionsToUpdate);

        await _listBuilder.UpdateListUnionDescriptions(existingListUnion.ProportionalElectionListUnionEntries.Select(x => x.ProportionalElectionListId));

        await _eventLogger.LogProportionalElectionListUnionEvent(eventData, existingListUnion);
    }

    public async Task Process(ProportionalElectionListUnionEntriesUpdated eventData)
    {
        var listUnionKey = eventData.ProportionalElectionListUnionEntries.ProportionalElectionListUnionId;
        var listUnionId = GuidParser.Parse(listUnionKey);

        var listUnion = await _listUnionRepo.Query()
            .AsSplitQuery()
            .Include(lu => lu.ProportionalElection)
            .Include(x => x.ProportionalElectionListUnionEntries)
            .Include(lu => lu.ProportionalElectionSubListUnions)
            .FirstOrDefaultAsync(x => x.Id == listUnionId)
            ?? throw new EntityNotFoundException(listUnionId);

        var newListIds = eventData.ProportionalElectionListUnionEntries.ProportionalElectionListIds.Select(GuidParser.Parse).ToList();
        var entries = newListIds.ConvertAll(listId => new ProportionalElectionListUnionEntry
        {
            ProportionalElectionListId = listId,
            ProportionalElectionListUnionId = listUnionId,
        });

        await _proportionalElectionListUnionEntryRepo.Replace(listUnionId, entries);

        // delete main list id in sub list union if it isn't in the new entries
        if (listUnion.IsSubListUnion
            && listUnion.ProportionalElectionMainListId.HasValue
            && !newListIds.Contains(listUnion.ProportionalElectionMainListId.Value))
        {
            listUnion.ProportionalElectionMainListId = null;
            await _listUnionRepo.Update(listUnion);
        }

        if (!listUnion.IsSubListUnion)
        {
            await UpdateSubListUnionsByRootListUnionEntries(listUnion, entries);
        }

        var touchedListIds = listUnion.ProportionalElectionListUnionEntries.Select(x => x.ProportionalElectionListId)
            .Concat(newListIds)
            .ToHashSet();
        await _listBuilder.UpdateListUnionDescriptions(touchedListIds);

        await _eventLogger.LogProportionalElectionListUnionEvent(eventData, listUnion);
    }

    public async Task Process(ProportionalElectionListUnionMainListUpdated eventData)
    {
        var listUnionId = GuidParser.Parse(eventData.ProportionalElectionListUnionId);

        var listUnion = await _listUnionRepo.Query()
            .AsSplitQuery()
            .Include(lu => lu.ProportionalElection)
            .Include(x => x.ProportionalElectionListUnionEntries)
            .FirstOrDefaultAsync(x => x.Id == listUnionId)
            ?? throw new EntityNotFoundException(listUnionId);

        listUnion.ProportionalElectionMainListId = string.IsNullOrEmpty(eventData.ProportionalElectionMainListId)
            ? null
            : GuidParser.Parse(eventData.ProportionalElectionMainListId);
        listUnion.ProportionalElectionMainList = null;

        await _listUnionRepo.Update(listUnion);

        await _listBuilder.UpdateListUnionDescriptions(listUnion.ProportionalElectionListUnionEntries.Select(x => x.ProportionalElectionListId));

        await _eventLogger.LogProportionalElectionListUnionEvent(eventData, listUnion);
    }

    public async Task Process(ProportionalElectionCandidateCreated eventData)
    {
        var model = _mapper.Map<ProportionalElectionCandidate>(eventData.ProportionalElectionCandidate);
        TruncateCandidateNumber(model);

        // old events don't contain a country
        if (string.IsNullOrEmpty(model.Country))
        {
            model.Country = CountryUtils.SwissCountryIso;
        }

        await _candidateRepo.Create(model);

        var candidate = await GetCandidate(model.Id);
        await UpdateListCandidateCount(candidate.ProportionalElectionList, true, candidate.Accumulated);
        await _eventLogger.LogProportionalElectionCandidateEvent(eventData, candidate);
    }

    public async Task Process(ProportionalElectionCandidateUpdated eventData)
    {
        var model = _mapper.Map<ProportionalElectionCandidate>(eventData.ProportionalElectionCandidate);
        TruncateCandidateNumber(model);

        // old events don't contain a country
        if (string.IsNullOrEmpty(model.Country))
        {
            model.Country = CountryUtils.SwissCountryIso;
        }

        var existingCandidate = await GetCandidate(model.Id);
        var list = existingCandidate.ProportionalElectionList;

        await _candidateRepo.Update(model);

        var addedAccumulation = !existingCandidate.Accumulated && model.Accumulated;
        var removedAccumulation = existingCandidate.Accumulated && !model.Accumulated;

        if (addedAccumulation)
        {
            await UpdateListCandidateCount(list, true, false);
        }
        else if (removedAccumulation)
        {
            await UpdateListCandidateCount(list, false, false);
        }

        // If the candidate accumulation was removed, decrease all affected candidate positions
        if (removedAccumulation)
        {
            var accumulatedPosition = existingCandidate.AccumulatedPosition;
            var candidatesToUpdate = await _candidateRepo.Query()
                .Where(c =>
                    c.ProportionalElectionListId == existingCandidate.ProportionalElectionListId
                    && (c.Position > accumulatedPosition || (c.Accumulated && c.AccumulatedPosition > accumulatedPosition)))
                .ToListAsync();

            DecreaseCandidatePositions(candidatesToUpdate, accumulatedPosition);
            await _candidateRepo.UpdateRange(candidatesToUpdate);
        }

        await _eventLogger.LogProportionalElectionCandidateEvent(eventData, model, list);
    }

    public async Task Process(ProportionalElectionCandidateAfterTestingPhaseUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.Id);
        var candidate = await GetCandidate(id);

        _mapper.Map(eventData, candidate);
        await _candidateRepo.Update(candidate);

        await _eventLogger.LogProportionalElectionCandidateEvent(eventData, candidate, candidate.ProportionalElectionList);
    }

    public async Task Process(ProportionalElectionCandidatesReordered eventData)
    {
        var listId = GuidParser.Parse(eventData.ProportionalElectionListId);
        var list = await _listRepo.Query()
            .Include(l => l.ProportionalElection)
            .Include(l => l.ProportionalElectionCandidates)
            .FirstOrDefaultAsync(l => l.Id == listId)
            ?? throw new EntityNotFoundException(listId);

        var grouped = eventData.CandidateOrders.Orders
            .GroupBy(o => o.Id)
            .ToDictionary(x => GuidParser.Parse(x.Key), x => x.Select(y => y.Position).OrderBy(y => y).ToList());

        foreach (var candidate in list.ProportionalElectionCandidates)
        {
            candidate.Position = grouped[candidate.Id][0];
            if (candidate.Accumulated)
            {
                candidate.AccumulatedPosition = grouped[candidate.Id][1];
            }
        }

        await _candidateRepo.UpdateRange(list.ProportionalElectionCandidates);
        await _eventLogger.LogProportionalElectionListEvent(eventData, list);
    }

    public async Task Process(ProportionalElectionCandidateDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.ProportionalElectionCandidateId);
        try
        {
            var existingCandidate = await GetCandidate(id);
            await UpdateListCandidateCount(existingCandidate.ProportionalElectionList, false, existingCandidate.Accumulated);
            await _candidateRepo.DeleteByKey(id);
            var candidatesToUpdate = await _candidateRepo.Query()
                .Where(c => c.ProportionalElectionListId == existingCandidate.ProportionalElectionListId
                    && c.Position > existingCandidate.Position)
                .ToListAsync();
            DecreaseCandidatePositions(candidatesToUpdate, existingCandidate.Position);
            if (existingCandidate.Accumulated)
            {
                DecreaseCandidatePositions(candidatesToUpdate, existingCandidate.AccumulatedPosition);
            }

            await _candidateRepo.UpdateRange(candidatesToUpdate);
            await _eventLogger.LogProportionalElectionCandidateEvent(eventData, existingCandidate);
        }
        catch (EntityNotFoundException)
        {
            // skip event processing to prevent race condition if proportional election candidate was deleted from other process.
            _logger.LogWarning("event 'ProportionalElectionCandidateDeleted' skipped. proportional election candidate {id} has already been deleted", id);
        }
    }

    public async Task Process(ProportionalElectionMandateAlgorithmUpdated eventData)
    {
        var electionId = GuidParser.Parse(eventData.ProportionalElectionId);
        var existingModel = await GetElection(electionId);

        existingModel.MandateAlgorithm = _mapper.Map<ProportionalElectionMandateAlgorithm>(eventData.MandateAlgorithm);
        await _repo.Update(existingModel);
        await _eventLogger.LogProportionalElectionEvent(eventData, existingModel);
    }

    private Task UpdateListCandidateCount(ProportionalElectionList list, bool added, bool accumulated)
    {
        var delta = added ? 1 : -1;
        list.CountOfCandidates += delta;
        if (accumulated)
        {
            list.CountOfCandidates += delta;
        }

        list.UpdateCandidateCountOk();
        return _listRepo.UpdateIgnoreRelations(list);
    }

    private async Task UpdateSubListUnionsByRootListUnionEntries(
        ProportionalElectionListUnion rootListUnion,
        IEnumerable<ProportionalElectionListUnionEntry> rootListUnionEntries)
    {
        var subListUnions = rootListUnion.ProportionalElectionSubListUnions;
        var rootEntryListIds = rootListUnionEntries.Select(e => e.ProportionalElectionListId).ToList();

        await _proportionalElectionListUnionEntryRepo.DeleteSubListUnionListEntriesByRootListIds(
            subListUnions.Select(lu => lu.Id).ToList(),
            rootEntryListIds);

        var subListUnionsWithRemovedRootListUnionEntry = subListUnions
            .Where(lu => lu.ProportionalElectionMainListId.HasValue && !rootEntryListIds.Contains(lu.ProportionalElectionMainListId.Value))
            .ToList();

        foreach (var subListUnion in subListUnionsWithRemovedRootListUnionEntry)
        {
            subListUnion.ProportionalElectionMainListId = null;
        }

        await _listUnionRepo.UpdateRangeIgnoreRelations(subListUnionsWithRemovedRootListUnionEntry);
    }

    private void DecreaseCandidatePositions(IEnumerable<ProportionalElectionCandidate> candidates, int fromPosition)
    {
        foreach (var candidate in candidates.Where(c => c.Position > fromPosition))
        {
            candidate.Position--;
            if (candidate.Accumulated)
            {
                candidate.AccumulatedPosition--;
            }
        }
    }

    private async Task<ProportionalElection> GetElection(Guid id)
    {
        return await _repo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);
    }

    private async Task<ProportionalElectionList> GetList(Guid listId)
    {
        return await _listRepo.Query()
            .Include(l => l.ProportionalElection)
            .FirstOrDefaultAsync(l => l.Id == listId)
            ?? throw new EntityNotFoundException(listId);
    }

    private async Task<ProportionalElectionListUnion> GetListUnion(Guid listUnionId)
    {
        return await _listUnionRepo.Query()
            .Include(lu => lu.ProportionalElection)
            .FirstOrDefaultAsync(lu => lu.Id == listUnionId)
            ?? throw new EntityNotFoundException(listUnionId);
    }

    private async Task<ProportionalElectionCandidate> GetCandidate(Guid candidateId)
    {
        return await _candidateRepo.Query()
            .Include(c => c.ProportionalElectionList.ProportionalElection)
            .FirstOrDefaultAsync(c => c.Id == candidateId)
            ?? throw new EntityNotFoundException(candidateId);
    }

    private void TruncateCandidateNumber(ProportionalElectionCandidate candidate)
    {
        if (candidate.Number.Length <= 10)
        {
            return;
        }

        // old events can contain a number which is longer than 10 chars
        candidate.Number = candidate.Number[..10];
    }
}
