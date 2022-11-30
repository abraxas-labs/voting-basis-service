// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Google.Protobuf;
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

public class ContestProcessor :
    IEventProcessor<ContestCreated>,
    IEventProcessor<ContestUpdated>,
    IEventProcessor<ContestDeleted>,
    IEventProcessor<ContestsMerged>,
    IEventProcessor<ContestTestingPhaseEnded>,
    IEventProcessor<ContestPastLocked>,
    IEventProcessor<ContestPastUnlocked>,
    IEventProcessor<ContestArchived>,
    IEventProcessor<ContestArchiveDateUpdated>,
    IEventProcessor<ContestCountingCircleOptionsUpdated>
{
    private readonly IDbRepository<DataContext, Contest> _repo;
    private readonly IMapper _mapper;
    private readonly EventLoggerAdapter _eventLogger;
    private readonly ContestCountingCircleOptionsReplacer _contestCountingCircleOptionsReplacer;
    private readonly MessageProducerBuffer _messageProducerBuffer;

    public ContestProcessor(
        IMapper mapper,
        IDbRepository<DataContext, Contest> repo,
        EventLoggerAdapter eventLogger,
        ContestCountingCircleOptionsReplacer contestCountingCircleOptionsReplacer,
        MessageProducerBuffer messageProducerBuffer)
    {
        _mapper = mapper;
        _repo = repo;
        _eventLogger = eventLogger;
        _contestCountingCircleOptionsReplacer = contestCountingCircleOptionsReplacer;
        _messageProducerBuffer = messageProducerBuffer;
    }

    public async Task Process(ContestCreated eventData)
    {
        var model = _mapper.Map<Contest>(eventData.Contest);
        model.State = ContestState.TestingPhase;
        await _repo.Create(model);
        await _contestCountingCircleOptionsReplacer.Replace(model, false);
        await _eventLogger.LogContestEvent(eventData, model);
        PublishContestEventMessage(model, EntityState.Added);
    }

    public async Task Process(ContestUpdated eventData)
    {
        var model = _mapper.Map<Contest>(eventData.Contest);

        var existingContest = await _repo.GetByKey(model.Id)
            ?? throw new EntityNotFoundException(model.Id);

        await _repo.Update(model);

        if (existingContest.EVoting != model.EVoting || existingContest.DomainOfInfluenceId != model.DomainOfInfluenceId)
        {
            await _contestCountingCircleOptionsReplacer.Replace(model);
        }

        await _eventLogger.LogContestEvent(eventData, model);
        PublishContestEventMessage(model, EntityState.Modified);
    }

    public async Task Process(ContestDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.ContestId);
        var existing = await _repo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);

        await _repo.DeleteByKey(id);
        await _eventLogger.LogContestEvent(eventData, existing);
        PublishContestEventMessage(existing, EntityState.Deleted);
    }

    public async Task Process(ContestsMerged eventData)
    {
        var id = GuidParser.Parse(eventData.MergedId);
        var contest = await _repo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);

        await _eventLogger.LogContestEvent(eventData, contest);
    }

    public Task Process(ContestTestingPhaseEnded eventData) => UpdateState(eventData.ContestId, ContestState.Active, eventData);

    public Task Process(ContestPastLocked eventData) => UpdateState(eventData.ContestId, ContestState.PastLocked, eventData);

    public Task Process(ContestArchived eventData)
    {
        return UpdateState(
            eventData.ContestId,
            ContestState.Archived,
            eventData,
            c =>
            {
                // the date of the event can be before the archive per date
                // if an archive date is set in the future but the user selects archive now.
                var eventDate = eventData.EventInfo.Timestamp.ToDateTime();
                if (c.ArchivePer == null || c.ArchivePer > eventDate)
                {
                    c.ArchivePer = eventDate;
                }
            });
    }

    public Task Process(ContestPastUnlocked eventData) => UpdateState(
        eventData.ContestId,
        ContestState.PastUnlocked,
        eventData,
        c => c.PastLockPer = eventData.EventInfo.Timestamp.ToDateTime().NextUtcDate(true));

    public async Task Process(ContestArchiveDateUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.ContestId);

        var contest = await _repo.GetByKey(id)
                      ?? throw new EntityNotFoundException(id);

        contest.ArchivePer = eventData.ArchivePer?.ToDateTime();
        await _repo.Update(contest);
        await _eventLogger.LogContestEvent(eventData, contest);
    }

    public async Task Process(ContestCountingCircleOptionsUpdated eventData)
    {
        var contestId = GuidParser.Parse(eventData.ContestId);
        var contest = await _repo.Query()
            .Include(x => x.CountingCircleOptions)
            .FirstOrDefaultAsync(x => x.Id == contestId)
            ?? throw new EntityNotFoundException(nameof(Contest), contestId);

        var optionsByCcId = contest.CountingCircleOptions.ToDictionary(x => x.CountingCircleId);
        foreach (var option in eventData.Options)
        {
            var ccId = GuidParser.Parse(option.CountingCircleId);
            if (!optionsByCcId.TryGetValue(ccId, out var existingOption))
            {
                throw new ValidationException($"contest counting circle option not found for contest {contestId} and ccId {ccId}");
            }

            existingOption.EVoting = option.EVoting;
        }

        await _repo.Update(contest);
        await _eventLogger.LogContestEvent(eventData, contest);
    }

    private async Task UpdateState<T>(string key, ContestState newState, T eventData, Action<Contest>? customizer = null)
        where T : IMessage<T>
    {
        var id = GuidParser.Parse(key);

        var contest = await _repo.GetByKey(id)
                      ?? throw new EntityNotFoundException(id);

        contest.State = newState;
        customizer?.Invoke(contest);
        await _repo.Update(contest);
        await _eventLogger.LogContestEvent(eventData, contest);
    }

    private void PublishContestEventMessage(Contest contest, EntityState state)
    {
        // Publish a contest change message, so that other service instances can refresh their in-memory cache.
        _messageProducerBuffer.Add(new ContestOverviewChangeMessage(contest.CreateBaseEntityEvent(state)));
    }
}
