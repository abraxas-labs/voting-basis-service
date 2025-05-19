// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Messaging;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Store;
using Voting.Lib.Messaging;

namespace Voting.Basis.Core.Services.Read;

public class EventLogReader
{
    private readonly IDbRepository<DataContext, EventLog> _repo;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAuth _auth;
    private readonly IValidator<Pageable> _pageableValidator;
    private readonly MessageConsumerHub<EventProcessedMessage> _eventProcessedHub;

    public EventLogReader(
        IDbRepository<DataContext, EventLog> repo,
        IServiceProvider serviceProvider,
        IAuth auth,
        IValidator<Pageable> pageableValidator,
        MessageConsumerHub<EventProcessedMessage> eventProcessedHub)
    {
        _repo = repo;
        _serviceProvider = serviceProvider;
        _auth = auth;
        _pageableValidator = pageableValidator;
        _eventProcessedHub = eventProcessedHub;
    }

    public async Task<Page<EventLog>> List(Pageable pageable)
    {
        _pageableValidator.ValidateAndThrow(pageable);

        var query = _repo.Query();

        if (_auth.HasPermission(Permissions.EventLog.ReadSameTenant))
        {
            var tenantId = _auth.Tenant.Id;
            query = query.Where(e => e.EventTenant!.TenantId == tenantId);
        }

        return await query
            .Include(e => e.EventTenant)
            .Include(e => e.EventUser)
            .OrderByDescending(e => e.Timestamp)
            .ToPageAsync(pageable);
    }

    public async Task Watch(
        IReadOnlyCollection<EventFilter> filters,
        Func<string, EventProcessedMessage, Task> listener,
        CancellationToken cancellationToken)
    {
        var permissions = _serviceProvider.GetRequiredService<PermissionAccessor>();
        await _eventProcessedHub.Listen(
            permissions.CanRead,
            e => Task.WhenAll(filters.Where(f => f.Filter(e)).Select(f => listener(f.Id, e))),
            cancellationToken);
    }

    public record EventFilter(
        string Id,
        IReadOnlySet<string> EventTypes,
        Guid? ContestId)
    {
        public bool Filter(EventProcessedMessage e)
        {
            return EventTypes.Contains(e.EventType) && (!ContestId.HasValue || e.ContestId == ContestId);
        }
    }
}
