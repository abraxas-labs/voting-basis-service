// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1.Models;
using Abraxas.Voting.Basis.Services.V1.Requests;
using AutoMapper;
using Grpc.Core;
using Voting.Basis.Core.Messaging;
using Voting.Basis.Core.Services.Read;
using Voting.Lib.Common;
using Voting.Lib.Iam.Authorization;
using Pageable = Voting.Lib.Database.Models.Pageable;
using Permissions = Voting.Basis.Core.Auth.Permissions;
using ServiceBase = Abraxas.Voting.Basis.Services.V1.EventLogService.EventLogServiceBase;

namespace Voting.Basis.Services;

public class EventLogService : ServiceBase
{
    private readonly EventLogReader _eventLogReader;
    private readonly IMapper _mapper;

    public EventLogService(EventLogReader eventLogReader, IMapper mapper)
    {
        _eventLogReader = eventLogReader;
        _mapper = mapper;
    }

    [AuthorizeAnyPermission(Permissions.EventLog.ReadSameTenant, Permissions.EventLog.ReadAll)]
    public override async Task<EventLogsPage> List(
        ListEventLogsRequest request,
        ServerCallContext context)
    {
        var pageable = _mapper.Map<Pageable>(request.Pageable);
        var eventLogs = await _eventLogReader.List(pageable);
        return _mapper.Map<EventLogsPage>(eventLogs);
    }

    [AuthorizePermission(Permissions.EventLog.Watch)]
    public override Task Watch(WatchEventsRequest request, IServerStreamWriter<Event> responseStream, ServerCallContext context)
    {
        var filters = request.Filters.Select(f => new EventLogReader.EventFilter(
            f.Id,
            f.Types_.ToHashSet(),
            GuidParser.ParseNullable(f.ContestId)))
            .ToList();

        Task Listener(string filterId, EventProcessedMessage e)
        {
            return responseStream.WriteAsync(new Event
            {
                Type = e.EventType,
                FilterId = filterId,
                AggregateId = e.AggregateId.ToString(),
                EntityId = e.EntityId?.ToString() ?? string.Empty,
                ContestId = e.ContestId?.ToString() ?? string.Empty,
                PoliticalBusinessId = e.PoliticalBusinessId?.ToString() ?? string.Empty,
            });
        }

        return _eventLogReader.Watch(filters, Listener, context.CancellationToken);
    }
}
