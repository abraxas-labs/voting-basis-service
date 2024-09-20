// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voting.Basis.Core.Configuration;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.EventSignature;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;

namespace Voting.Basis.Core.EventSignature;

public class EventSignatureAggregateRepositoryHandler : IAggregateRepositoryHandler, IDisposable
{
    private readonly EventSignatureService _eventSignatureService;
    private readonly ContestCache _contestCache;
    private readonly EventSignatureConfig _eventSignatureConfig;

    private readonly IReadOnlyCollection<Type> _nonEventSignatureBusinessAggregateTypes = new[]
    {
        typeof(CountingCircleAggregate),
        typeof(DomainOfInfluenceAggregate),
        typeof(ContestEventSignatureAggregate),
        typeof(CantonSettingsAggregate),
        typeof(PoliticalAssemblyAggregate),
    };

    private IDisposable? _writeLock;

    public EventSignatureAggregateRepositoryHandler(EventSignatureService eventSignatureService, ContestCache contestCache, EventSignatureConfig eventSignatureConfig)
    {
        _eventSignatureService = eventSignatureService;
        _contestCache = contestCache;
        _eventSignatureConfig = eventSignatureConfig;
    }

    public async Task BeforeSaved<TAggregate>(TAggregate aggregate)
        where TAggregate : BaseEventSourcingAggregate
    {
        if (IsEventSignatureDisabled(aggregate))
        {
            return;
        }

        _writeLock = _contestCache.BatchWrite();
        await _eventSignatureService.FillBusinessMetadata(aggregate);
    }

    public Task AfterSaved<TAggregate>(TAggregate aggregate, IReadOnlyCollection<IDomainEvent> publishedEvents)
        where TAggregate : BaseEventSourcingAggregate
    {
        if (IsEventSignatureDisabled(aggregate))
        {
            return Task.CompletedTask;
        }

        _eventSignatureService.UpdateSignedEventCount(publishedEvents);
        _writeLock?.Dispose();
        _writeLock = null;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _writeLock?.Dispose();
    }

    private bool IsEventSignatureDisabled<TAggregate>(TAggregate aggregate)
         where TAggregate : BaseEventSourcingAggregate
    {
        return _nonEventSignatureBusinessAggregateTypes.Contains(aggregate.GetType()) || !_eventSignatureConfig.Enabled;
    }
}
