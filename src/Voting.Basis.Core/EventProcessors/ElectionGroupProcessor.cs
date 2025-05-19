// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.EventProcessors;

public class ElectionGroupProcessor :
    IEventProcessor<ElectionGroupCreated>,
    IEventProcessor<ElectionGroupDeleted>
{
    private readonly IDbRepository<DataContext, ElectionGroup> _repo;
    private readonly IMapper _mapper;
    private readonly EventLoggerAdapter _eventLogger;

    public ElectionGroupProcessor(
        IDbRepository<DataContext, ElectionGroup> repo,
        IMapper mapper,
        EventLoggerAdapter eventLogger)
    {
        _repo = repo;
        _mapper = mapper;
        _eventLogger = eventLogger;
    }

    public async Task Process(ElectionGroupCreated eventData)
    {
        var model = _mapper.Map<ElectionGroup>(eventData.ElectionGroup);
        await _repo.Create(model);

        var existingElectionGroup = await GetElectionGroup(model.Id);
        await _eventLogger.LogElectionGroupEvent(eventData, existingElectionGroup);
    }

    public async Task Process(ElectionGroupDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.ElectionGroupId);
        var existingModel = await GetElectionGroup(id);

        await _repo.DeleteByKey(id);
        await _eventLogger.LogElectionGroupEvent(eventData, existingModel);
    }

    private async Task<ElectionGroup> GetElectionGroup(Guid id)
    {
        return await _repo.Query()
            .Include(eg => eg.PrimaryMajorityElection)
            .FirstOrDefaultAsync(eg => eg.Id == id)
            ?? throw new EntityNotFoundException(id);
    }
}
