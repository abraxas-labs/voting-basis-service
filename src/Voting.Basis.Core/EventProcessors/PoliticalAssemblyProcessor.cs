// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.EventProcessors;

public class PoliticalAssemblyProcessor :
    IEventProcessor<PoliticalAssemblyCreated>,
    IEventProcessor<PoliticalAssemblyUpdated>,
    IEventProcessor<PoliticalAssemblyDeleted>
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
}
