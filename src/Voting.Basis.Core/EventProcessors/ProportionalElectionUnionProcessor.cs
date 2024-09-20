// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

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
    private readonly MessageProducerBuffer _messageProducerBuffer;

    public ProportionalElectionUnionProcessor(
        IDbRepository<DataContext, ProportionalElectionUnion> repo,
        ProportionalElectionUnionEntryRepo entriesRepo,
        IMapper mapper,
        ProportionalElectionUnionListBuilder unionListBuilder,
        EventLoggerAdapter eventLogger,
        MessageProducerBuffer messageProducerBuffer)
    {
        _repo = repo;
        _entriesRepo = entriesRepo;
        _mapper = mapper;
        _unionListBuilder = unionListBuilder;
        _eventLogger = eventLogger;
        _messageProducerBuffer = messageProducerBuffer;
    }

    public async Task Process(ProportionalElectionUnionCreated eventData)
    {
        var model = _mapper.Map<ProportionalElectionUnion>(eventData.ProportionalElectionUnion);
        await _repo.Create(model);
        await _eventLogger.LogProportionalElectionUnionEvent(eventData, model);
        PublishContestDetailsChangeMessage(model, EntityState.Added);
    }

    public async Task Process(ProportionalElectionUnionUpdated eventData)
    {
        var model = _mapper.Map<ProportionalElectionUnion>(eventData.ProportionalElectionUnion);

        if (!await _repo.ExistsByKey(model.Id))
        {
            throw new EntityNotFoundException(model.Id);
        }

        await _repo.Update(model);
        await _eventLogger.LogProportionalElectionUnionEvent(eventData, model);
        PublishContestDetailsChangeMessage(model, EntityState.Modified);
    }

    public async Task Process(ProportionalElectionUnionEntriesUpdated eventData)
    {
        var proportionalElectionUnionId = GuidParser.Parse(eventData.ProportionalElectionUnionEntries.ProportionalElectionUnionId);

        var existingModel = await _repo.GetByKey(proportionalElectionUnionId)
            ?? throw new EntityNotFoundException(proportionalElectionUnionId);

        var models = eventData.ProportionalElectionUnionEntries.ProportionalElectionIds.Select(electionId =>
            new ProportionalElectionUnionEntry
            {
                ProportionalElectionUnionId = proportionalElectionUnionId,
                ProportionalElectionId = GuidParser.Parse(electionId),
            }).ToList();

        await _entriesRepo.Replace(proportionalElectionUnionId, models);
        await _unionListBuilder.RebuildLists(
            proportionalElectionUnionId,
            models.ConvertAll(e => e.ProportionalElectionId));
        await _eventLogger.LogProportionalElectionUnionEvent(eventData, existingModel);
    }

    public async Task Process(ProportionalElectionUnionDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.ProportionalElectionUnionId);

        var existingModel = await _repo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);

        await _repo.DeleteByKey(id);
        await _eventLogger.LogProportionalElectionUnionEvent(eventData, existingModel);
        PublishContestDetailsChangeMessage(existingModel, EntityState.Deleted);
    }

    public async Task Process(ProportionalElectionUnionToNewContestMoved eventData)
    {
        var id = GuidParser.Parse(eventData.ProportionalElectionUnionId);

        var existingModel = await _repo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);

        existingModel.ContestId = GuidParser.Parse(eventData.NewContestId);
        await _repo.Update(existingModel);
        await _eventLogger.LogProportionalElectionUnionEvent(eventData, existingModel);
    }

    private void PublishContestDetailsChangeMessage(ProportionalElectionUnion proportionalElectionUnion, EntityState state)
    {
        _messageProducerBuffer.Add(new ContestDetailsChangeMessage(politicalBusinessUnion: proportionalElectionUnion.CreateBaseEntityEvent(state)));
    }
}
