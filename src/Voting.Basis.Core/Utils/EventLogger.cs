// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1.Data;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Messaging;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Subscribe;
using Voting.Lib.Messaging;

namespace Voting.Basis.Core.Utils;

public class EventLogger
{
    private static readonly JsonFormatter JsonFormatter = new(new JsonFormatter.Settings(true));

    private readonly IDbRepository<DataContext, EventLog> _eventLogRepo;
    private readonly IDbRepository<DataContext, EventLogUser> _eventUserRepo;
    private readonly IDbRepository<DataContext, EventLogTenant> _eventTenantRepo;
    private readonly EventProcessorContextAccessor _eventContextAccessor;
    private readonly MessageProducerBuffer _messageBuffer;

    public EventLogger(
        IDbRepository<DataContext, EventLog> eventLogRepo,
        IDbRepository<DataContext, EventLogUser> eventUserRepo,
        IDbRepository<DataContext, EventLogTenant> eventTenantRepo,
        EventProcessorContextAccessor eventContextAccessor,
        MessageProducerBuffer messageBuffer)
    {
        _eventLogRepo = eventLogRepo;
        _eventUserRepo = eventUserRepo;
        _eventTenantRepo = eventTenantRepo;
        _eventContextAccessor = eventContextAccessor;
        _messageBuffer = messageBuffer;
    }

    internal async Task LogEvent<T>(T eventData, EventLog eventLog)
        where T : IMessage<T>
    {
        var eventInfoProp = eventData.GetType().GetProperty(nameof(EventInfo))
            ?? throw new ArgumentException("Event has no EventInfo field", nameof(eventData));
        var eventInfo = eventInfoProp.GetValue(eventData) as EventInfo
            ?? throw new ArgumentException("Could not retrieve event info value", nameof(eventData));

        // Since we extract the event info values, we remove the field temporarily, so that the JSON doesn't get too huge
        eventInfoProp.SetValue(eventData, null);
        eventLog.EventContent = JsonFormatter.Format(eventData);
        eventInfoProp.SetValue(eventData, eventInfo);

        eventLog.EventName = eventData.Descriptor.Name;
        eventLog.Timestamp = eventInfo.Timestamp.ToDateTime();
        eventLog.EventUser = await ResolveEventUser(eventInfo.User);
        eventLog.EventTenant = await ResolveEventTenant(eventInfo.Tenant);
        await _eventLogRepo.Create(eventLog);

        // only publish live update messages if the subscription is up to date.
        // These messages are not needed for replays/catch-ups and only result in additional load.
        // Also, these messages are not mission-critical.
        if (!_eventContextAccessor.Context.IsCatchUp)
        {
            _messageBuffer.Add(new EventProcessedMessage(
                eventData.Descriptor.FullName,
                eventLog.EventTenant.TenantId,
                eventLog.AggregateId ?? throw new InvalidOperationException(nameof(eventLog.AggregateId) + " needs to be set"),
                eventLog.EntityId,
                eventLog.ContestId,
                eventLog.PoliticalBusinessId,
                eventLog.DomainOfInfluenceId,
                eventLog.CountingCircleId));
        }
    }

    private async Task<EventLogUser> ResolveEventUser(EventInfoUser? user)
    {
        if (user == null)
        {
            throw new InvalidOperationException("Event info user may not be null");
        }

        var existingEventUser = await _eventUserRepo.Query()
            .AsTracking()
            .Where(u => u.UserId == user.Id)
            .FirstOrDefaultAsync();

        return existingEventUser ?? new EventLogUser
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Username = user.Username,
        };
    }

    private async Task<EventLogTenant> ResolveEventTenant(EventInfoTenant? tenant)
    {
        if (tenant == null)
        {
            throw new InvalidOperationException("Event info tenant may not be null");
        }

        var existingEventTenant = await _eventTenantRepo.Query()
            .AsTracking()
            .Where(u => u.TenantId == tenant.Id)
            .FirstOrDefaultAsync();

        return existingEventTenant ?? new EventLogTenant
        {
            TenantId = tenant.Id,
            TenantName = tenant.Name,
        };
    }
}
