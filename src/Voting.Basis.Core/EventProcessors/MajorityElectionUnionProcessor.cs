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

public class MajorityElectionUnionProcessor :
    IEventProcessor<MajorityElectionUnionCreated>,
    IEventProcessor<MajorityElectionUnionUpdated>,
    IEventProcessor<MajorityElectionUnionDeleted>,
    IEventProcessor<MajorityElectionUnionToNewContestMoved>,
    IEventProcessor<MajorityElectionUnionEntriesUpdated>
{
    private readonly IDbRepository<DataContext, MajorityElectionUnion> _repo;
    private readonly MajorityElectionUnionEntryRepo _entriesRepo;
    private readonly IMapper _mapper;
    private readonly EventLoggerAdapter _eventLogger;

    public MajorityElectionUnionProcessor(
        IDbRepository<DataContext, MajorityElectionUnion> repo,
        MajorityElectionUnionEntryRepo entriesRepo,
        IMapper mapper,
        EventLoggerAdapter eventLogger)
    {
        _repo = repo;
        _entriesRepo = entriesRepo;
        _mapper = mapper;
        _eventLogger = eventLogger;
    }

    public async Task Process(MajorityElectionUnionCreated eventData)
    {
        var model = _mapper.Map<MajorityElectionUnion>(eventData.MajorityElectionUnion);
        await _repo.Create(model);
        await _eventLogger.LogMajorityElectionUnionEvent(eventData, await GetUnion(model.Id));
    }

    public async Task Process(MajorityElectionUnionUpdated eventData)
    {
        var model = _mapper.Map<MajorityElectionUnion>(eventData.MajorityElectionUnion);
        var existingModel = await GetUnion(model.Id);

        await _repo.Update(model);
        await _eventLogger.LogMajorityElectionUnionEvent(eventData, model, existingModel.Contest.DomainOfInfluenceId);
    }

    public async Task Process(MajorityElectionUnionEntriesUpdated eventData)
    {
        var majorityElectionUnionId = GuidParser.Parse(eventData.MajorityElectionUnionEntries.MajorityElectionUnionId);

        var existingModel = await GetUnion(majorityElectionUnionId);

        var models = eventData.MajorityElectionUnionEntries.MajorityElectionIds.Select(electionId =>
            new MajorityElectionUnionEntry
            {
                MajorityElectionUnionId = majorityElectionUnionId,
                MajorityElectionId = GuidParser.Parse(electionId),
            }).ToList();

        await _entriesRepo.Replace(majorityElectionUnionId, models);
        await _eventLogger.LogMajorityElectionUnionEvent(eventData, existingModel);
    }

    public async Task Process(MajorityElectionUnionDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.MajorityElectionUnionId);
        var existingModel = await GetUnion(id);

        await _repo.DeleteByKey(id);
        await _eventLogger.LogMajorityElectionUnionEvent(eventData, existingModel);
    }

    public async Task Process(MajorityElectionUnionToNewContestMoved eventData)
    {
        var id = GuidParser.Parse(eventData.MajorityElectionUnionId);
        await _repo.Query()
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.ContestId, GuidParser.Parse(eventData.NewContestId)));

        await _eventLogger.LogMajorityElectionUnionEvent(eventData, await GetUnion(id));
    }

    private async Task<MajorityElectionUnion> GetUnion(Guid id)
        => await _repo.Query()
               .Include(x => x.Contest)
               .FirstOrDefaultAsync(x => x.Id == id)
           ?? throw new EntityNotFoundException(nameof(MajorityElectionUnion), id);
}
