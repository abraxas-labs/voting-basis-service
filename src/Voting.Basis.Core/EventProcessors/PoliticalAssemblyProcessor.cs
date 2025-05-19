// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.EventProcessors;

public class PoliticalAssemblyProcessor :
    IEventProcessor<PoliticalAssemblyCreated>,
    IEventProcessor<PoliticalAssemblyUpdated>,
    IEventProcessor<PoliticalAssemblyDeleted>,
    IEventProcessor<PoliticalAssemblyPastLocked>,
    IEventProcessor<PoliticalAssemblyArchived>,
    IEventProcessor<PoliticalAssemblyArchiveDateUpdated>
{
    private readonly IDbRepository<DataContext, PoliticalAssembly> _repo;
    private readonly IMapper _mapper;
    private readonly EventLoggerAdapter _eventLogger;
    private readonly ILogger<PoliticalAssemblyProcessor> _logger;

    public PoliticalAssemblyProcessor(
        ILogger<PoliticalAssemblyProcessor> logger,
        IMapper mapper,
        IDbRepository<DataContext, PoliticalAssembly> repo,
        EventLoggerAdapter eventLogger)
    {
        _logger = logger;
        _mapper = mapper;
        _repo = repo;
        _eventLogger = eventLogger;
    }

    public async Task Process(PoliticalAssemblyCreated eventData)
    {
        var model = _mapper.Map<PoliticalAssembly>(eventData.PoliticalAssembly);
        await _repo.Create(model);
        await _eventLogger.LogPoliticalAssemblyEvent(eventData, model);
    }

    public async Task Process(PoliticalAssemblyUpdated eventData)
    {
        var model = _mapper.Map<PoliticalAssembly>(eventData.PoliticalAssembly);
        await _repo.Update(model);
        await _eventLogger.LogPoliticalAssemblyEvent(eventData, model);
    }

    public async Task Process(PoliticalAssemblyDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.PoliticalAssemblyId);
        var existing = await _repo.GetByKey(id);
        if (existing is null)
        {
            // skip event processing to prevent race condition if political assembly was deleted from other process.
            _logger.LogWarning("event 'PoliticalAssemblyDeleted' skipped. political assembly {id} has already been deleted", id);
            return;
        }

        await _repo.DeleteByKey(id);
        await _eventLogger.LogPoliticalAssemblyEvent(eventData, existing);
    }

    public async Task Process(PoliticalAssemblyPastLocked eventData) => await UpdateState(eventData.PoliticalAssemblyId, PoliticalAssemblyState.PastLocked, eventData);

    public Task Process(PoliticalAssemblyArchived eventData)
    {
        return UpdateState(
            eventData.PoliticalAssemblyId,
            PoliticalAssemblyState.Archived,
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

    public async Task Process(PoliticalAssemblyArchiveDateUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.PoliticalAssemblyId);
        var politicalAssembly = await GetPoliticalAssembly(id);

        politicalAssembly.ArchivePer = eventData.ArchivePer?.ToDateTime();
        await _repo.Update(politicalAssembly);
        await _eventLogger.LogPoliticalAssemblyEvent(eventData, politicalAssembly);
    }

    private async Task<PoliticalAssembly> GetPoliticalAssembly(Guid id) => await _repo.GetByKey(id)
        ?? throw new EntityNotFoundException(id);

    private async Task UpdateState<T>(string key, PoliticalAssemblyState newState, T eventData, Action<PoliticalAssembly>? customizer = null)
        where T : IMessage<T>
    {
        var id = GuidParser.Parse(key);
        var politicalAssembly = await GetPoliticalAssembly(id);

        politicalAssembly.State = newState;
        customizer?.Invoke(politicalAssembly);
        await _repo.Update(politicalAssembly);
        await _eventLogger.LogPoliticalAssemblyEvent(eventData, politicalAssembly);
    }
}
