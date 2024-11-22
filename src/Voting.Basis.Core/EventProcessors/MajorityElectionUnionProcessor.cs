// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Messaging.Extensions;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Messaging;

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
    private readonly MessageProducerBuffer _messageProducerBuffer;

    public MajorityElectionUnionProcessor(
        IDbRepository<DataContext, MajorityElectionUnion> repo,
        MajorityElectionUnionEntryRepo entriesRepo,
        IMapper mapper,
        EventLoggerAdapter eventLogger,
        MessageProducerBuffer messageProducerBuffer)
    {
        _repo = repo;
        _entriesRepo = entriesRepo;
        _mapper = mapper;
        _eventLogger = eventLogger;
        _messageProducerBuffer = messageProducerBuffer;
    }

    public async Task Process(MajorityElectionUnionCreated eventData)
    {
        var model = _mapper.Map<MajorityElectionUnion>(eventData.MajorityElectionUnion);
        await _repo.Create(model);
        await _eventLogger.LogMajorityElectionUnionEvent(eventData, model);
        PublishContestDetailsChangeMessage(model, EntityState.Added);
    }

    public async Task Process(MajorityElectionUnionUpdated eventData)
    {
        var model = _mapper.Map<MajorityElectionUnion>(eventData.MajorityElectionUnion);

        if (!await _repo.ExistsByKey(model.Id))
        {
            throw new EntityNotFoundException(model.Id);
        }

        await _repo.Update(model);
        await _eventLogger.LogMajorityElectionUnionEvent(eventData, model);

        var electionIds = _entriesRepo.Query()
            .Where(x => x.MajorityElectionUnionId == model.Id)
            .Select(x => x.MajorityElectionId)
            .ToList();

        PublishContestDetailsChangeMessage(model, EntityState.Modified, electionIds);
    }

    public async Task Process(MajorityElectionUnionEntriesUpdated eventData)
    {
        var majorityElectionUnionId = GuidParser.Parse(eventData.MajorityElectionUnionEntries.MajorityElectionUnionId);

        var existingModel = await _repo.GetByKey(majorityElectionUnionId)
            ?? throw new EntityNotFoundException(majorityElectionUnionId);

        var models = eventData.MajorityElectionUnionEntries.MajorityElectionIds.Select(electionId =>
            new MajorityElectionUnionEntry
            {
                MajorityElectionUnionId = majorityElectionUnionId,
                MajorityElectionId = GuidParser.Parse(electionId),
            }).ToList();

        await _entriesRepo.Replace(majorityElectionUnionId, models);
        await _eventLogger.LogMajorityElectionUnionEvent(eventData, existingModel);

        var electionIds = models.ConvertAll(x => x.MajorityElectionId);
        PublishContestDetailsChangeMessage(existingModel, EntityState.Modified, electionIds);
    }

    public async Task Process(MajorityElectionUnionDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.MajorityElectionUnionId);

        var existingModel = await _repo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);

        await _repo.DeleteByKey(id);
        await _eventLogger.LogMajorityElectionUnionEvent(eventData, existingModel);
        PublishContestDetailsChangeMessage(existingModel, EntityState.Deleted);
    }

    public async Task Process(MajorityElectionUnionToNewContestMoved eventData)
    {
        var id = GuidParser.Parse(eventData.MajorityElectionUnionId);

        var existingModel = await _repo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);

        existingModel.ContestId = GuidParser.Parse(eventData.NewContestId);
        await _repo.Update(existingModel);
        await _eventLogger.LogMajorityElectionUnionEvent(eventData, existingModel);
    }

    private void PublishContestDetailsChangeMessage(MajorityElectionUnion majorityElectionUnion, EntityState state, List<Guid>? electionIds = null)
    {
        _messageProducerBuffer.Add(new ContestDetailsChangeMessage(politicalBusinessUnion: majorityElectionUnion.CreateBaseEntityEvent(state, electionIds)));
    }
}
