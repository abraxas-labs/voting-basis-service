// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1.Models;
using Abraxas.Voting.Basis.Services.V1.Requests;
using AutoMapper;
using Grpc.Core;
using Voting.Basis.Core.Services.Read;
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

    [AuthorizePermission(Permissions.EventLog.Read)]
    public override async Task<EventLogsPage> List(
        ListEventLogsRequest request,
        ServerCallContext context)
    {
        var pageable = _mapper.Map<Pageable>(request.Pageable);
        var eventLogs = await _eventLogReader.List(pageable);
        return _mapper.Map<EventLogsPage>(eventLogs);
    }
}
