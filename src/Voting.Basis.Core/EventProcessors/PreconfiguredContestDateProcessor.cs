// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.EventProcessors;

public class PreconfiguredContestDateProcessor : IEventProcessor<PreconfiguredContestDateCreated>
{
    private readonly IDbRepository<DataContext, DateTime, PreconfiguredContestDate> _repo;
    private readonly IMapper _mapper;

    public PreconfiguredContestDateProcessor(
        IMapper mapper,
        IDbRepository<DataContext, DateTime, PreconfiguredContestDate> repo)
    {
        _mapper = mapper;
        _repo = repo;
    }

    public async Task Process(PreconfiguredContestDateCreated eventData)
    {
        var model = _mapper.Map<PreconfiguredContestDate>(eventData.PreconfiguredContestDate);
        await _repo.Create(model);
    }
}
