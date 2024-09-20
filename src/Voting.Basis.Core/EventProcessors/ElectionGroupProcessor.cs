// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Messaging.Extensions;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Messaging;
using EntityState = Voting.Basis.Core.Messaging.Messages.EntityState;

namespace Voting.Basis.Core.EventProcessors;

public class ElectionGroupProcessor :
    IEventProcessor<ElectionGroupCreated>,
    IEventProcessor<ElectionGroupUpdated>,
    IEventProcessor<ElectionGroupDeleted>
{
    private readonly IDbRepository<DataContext, ElectionGroup> _repo;
    private readonly IMapper _mapper;
    private readonly EventLoggerAdapter _eventLogger;
    private readonly MessageProducerBuffer _messageProducer;

    public ElectionGroupProcessor(
        IDbRepository<DataContext, ElectionGroup> repo,
        IMapper mapper,
        EventLoggerAdapter eventLogger,
        MessageProducerBuffer messageProducer)
    {
        _repo = repo;
        _mapper = mapper;
        _eventLogger = eventLogger;
        _messageProducer = messageProducer;
    }

    public async Task Process(ElectionGroupCreated eventData)
    {
        var model = _mapper.Map<ElectionGroup>(eventData.ElectionGroup);
        await _repo.Create(model);

        var existingElectionGroup = await GetElectionGroup(model.Id);
        await _eventLogger.LogElectionGroupEvent(eventData, existingElectionGroup);
        PublishContestDetailsChangeMessage(existingElectionGroup, EntityState.Added);
    }

    public async Task Process(ElectionGroupUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.ElectionGroupId);
        var existingModel = await GetElectionGroup(id);

        existingModel.Description = eventData.Description;
        await _repo.Update(existingModel);
        await _eventLogger.LogElectionGroupEvent(eventData, existingModel);
        PublishContestDetailsChangeMessage(existingModel, EntityState.Modified);
    }

    public async Task Process(ElectionGroupDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.ElectionGroupId);
        var existingModel = await GetElectionGroup(id);

        await _repo.DeleteByKey(id);
        await _eventLogger.LogElectionGroupEvent(eventData, existingModel);
        PublishContestDetailsChangeMessage(existingModel, EntityState.Deleted);
    }

    private async Task<ElectionGroup> GetElectionGroup(Guid id)
    {
        return await _repo.Query()
            .Include(eg => eg.PrimaryMajorityElection)
            .FirstOrDefaultAsync(eg => eg.Id == id)
            ?? throw new EntityNotFoundException(id);
    }

    private void PublishContestDetailsChangeMessage(
        ElectionGroup electionGroup,
        EntityState state)
    {
        _messageProducer.Add(new ContestDetailsChangeMessage(electionGroup: electionGroup.CreateBaseEntityEvent(state)));
    }
}
