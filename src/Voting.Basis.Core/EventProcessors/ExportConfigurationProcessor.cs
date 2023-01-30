// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using ExportProvider = Abraxas.Voting.Basis.Shared.V1.ExportProvider;

namespace Voting.Basis.Core.EventProcessors;

public class ExportConfigurationProcessor
    : IEventProcessor<ExportConfigurationCreated>,
        IEventProcessor<ExportConfigurationUpdated>,
        IEventProcessor<ExportConfigurationDeleted>
{
    private readonly IDbRepository<DataContext, ExportConfiguration> _repo;
    private readonly IMapper _mapper;
    private readonly EventLoggerAdapter _eventLogger;

    public ExportConfigurationProcessor(IDbRepository<DataContext, ExportConfiguration> repo, IMapper mapper, EventLoggerAdapter eventLogger)
    {
        _repo = repo;
        _mapper = mapper;
        _eventLogger = eventLogger;
    }

    public async Task Process(ExportConfigurationCreated eventData)
    {
        AdjustOldEvents(eventData.Configuration);
        var config = _mapper.Map<ExportConfiguration>(eventData.Configuration);
        await _repo.Create(config);
        await _eventLogger.LogDomainOfInfluenceEvent(eventData, config.DomainOfInfluenceId);
    }

    public async Task Process(ExportConfigurationUpdated eventData)
    {
        AdjustOldEvents(eventData.Configuration);
        var config = _mapper.Map<ExportConfiguration>(eventData.Configuration);
        await _repo.Update(config);
        await _eventLogger.LogDomainOfInfluenceEvent(eventData, config.DomainOfInfluenceId);
    }

    public async Task Process(ExportConfigurationDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.ConfigurationId);
        var config = await _repo.GetByKey(id)
                     ?? throw new EntityNotFoundException(id);
        await _repo.DeleteByKey(id);
        await _eventLogger.LogDomainOfInfluenceEvent(eventData, config.DomainOfInfluenceId);
    }

    private void AdjustOldEvents(ExportConfigurationEventData eventData)
    {
        // "Old" events that were created before the export provider was implemented need to be adjusted
        if (eventData.Provider == ExportProvider.Unspecified)
        {
            eventData.Provider = ExportProvider.Standard;
        }
    }
}
