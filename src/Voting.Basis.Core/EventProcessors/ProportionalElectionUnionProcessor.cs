// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.EventProcessors;

public class ProportionalElectionUnionProcessor :
    IEventProcessor<ProportionalElectionUnionCreated>,
    IEventProcessor<ProportionalElectionUnionUpdated>,
    IEventProcessor<ProportionalElectionUnionDeleted>,
    IEventProcessor<ProportionalElectionUnionToNewContestMoved>,
    IEventProcessor<ProportionalElectionUnionEntriesUpdated>
{
    private readonly IDbRepository<DataContext, ProportionalElectionUnion> _repo;
    private readonly ProportionalElectionUnionEntryRepo _entriesRepo;
    private readonly IMapper _mapper;
    private readonly ProportionalElectionUnionListBuilder _unionListBuilder;
    private readonly EventLoggerAdapter _eventLogger;

    public ProportionalElectionUnionProcessor(
        IDbRepository<DataContext, ProportionalElectionUnion> repo,
        ProportionalElectionUnionEntryRepo entriesRepo,
        IMapper mapper,
        ProportionalElectionUnionListBuilder unionListBuilder,
        EventLoggerAdapter eventLogger)
    {
        _repo = repo;
        _entriesRepo = entriesRepo;
        _mapper = mapper;
        _unionListBuilder = unionListBuilder;
        _eventLogger = eventLogger;
    }

    public async Task Process(ProportionalElectionUnionCreated eventData)
    {
        var model = _mapper.Map<ProportionalElectionUnion>(eventData.ProportionalElectionUnion);
        await _repo.Create(model);
        await _eventLogger.LogProportionalElectionUnionEvent(eventData, await GetUnion(model.Id));
    }

    public async Task Process(ProportionalElectionUnionUpdated eventData)
    {
        var model = _mapper.Map<ProportionalElectionUnion>(eventData.ProportionalElectionUnion);
        var existingModel = await GetUnion(model.Id);

        await _repo.Update(model);
        await _eventLogger.LogProportionalElectionUnionEvent(eventData, model, existingModel.Contest.DomainOfInfluenceId);
    }

    public async Task Process(ProportionalElectionUnionEntriesUpdated eventData)
    {
        var proportionalElectionUnionId = GuidParser.Parse(eventData.ProportionalElectionUnionEntries.ProportionalElectionUnionId);
        var existingModel = await GetUnion(proportionalElectionUnionId);

        var models = eventData.ProportionalElectionUnionEntries.ProportionalElectionIds.Select(electionId =>
            new ProportionalElectionUnionEntry
            {
                ProportionalElectionUnionId = proportionalElectionUnionId,
                ProportionalElectionId = GuidParser.Parse(electionId),
            }).ToList();

        var electionIds = models.ConvertAll(e => e.ProportionalElectionId);

        await _entriesRepo.Replace(proportionalElectionUnionId, models);
        await _unionListBuilder.RebuildLists(
            proportionalElectionUnionId,
            electionIds);
        await _eventLogger.LogProportionalElectionUnionEvent(eventData, existingModel);
    }

    public async Task Process(ProportionalElectionUnionDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.ProportionalElectionUnionId);
        var existingModel = await GetUnion(id);

        await _repo.DeleteByKey(id);
        await _eventLogger.LogProportionalElectionUnionEvent(eventData, existingModel);
    }

    public async Task Process(ProportionalElectionUnionToNewContestMoved eventData)
    {
        var id = GuidParser.Parse(eventData.ProportionalElectionUnionId);
        await _repo.Query()
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.ContestId, GuidParser.Parse(eventData.NewContestId)));

        await _eventLogger.LogProportionalElectionUnionEvent(eventData, await GetUnion(id));
    }

    private async Task<ProportionalElectionUnion> GetUnion(Guid id)
        => await _repo.Query()
               .Include(x => x.Contest)
               .FirstOrDefaultAsync(x => x.Id == id)
           ?? throw new EntityNotFoundException(nameof(ProportionalElectionUnion), id);
}
