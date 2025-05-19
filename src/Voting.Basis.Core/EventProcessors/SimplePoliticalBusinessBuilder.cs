// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.EventProcessors;

public class SimplePoliticalBusinessBuilder<TPoliticalBusiness>
    where TPoliticalBusiness : PoliticalBusiness
{
    private readonly IDbRepository<DataContext, SimplePoliticalBusiness> _politicalBusinessRepo;
    private readonly IMapper _mapper;

    public SimplePoliticalBusinessBuilder(
        IDbRepository<DataContext, SimplePoliticalBusiness> politicalBusinessRepo,
        IMapper mapper)
    {
        _politicalBusinessRepo = politicalBusinessRepo;
        _mapper = mapper;
    }

    public async Task Create(TPoliticalBusiness politicalBusiness)
    {
        var simplePoliticalBusiness = _mapper.Map<SimplePoliticalBusiness>(politicalBusiness);
        await _politicalBusinessRepo.Create(simplePoliticalBusiness);
    }

    public async Task Update(TPoliticalBusiness politicalBusiness)
    {
        var simplePoliticalBusiness = _mapper.Map<SimplePoliticalBusiness>(politicalBusiness);
        await _politicalBusinessRepo.Update(simplePoliticalBusiness);
    }

    public async Task UpdateSubTypeIfNecessary(TPoliticalBusiness politicalBusiness)
    {
        var simplePoliticalBusiness = _mapper.Map<SimplePoliticalBusiness>(politicalBusiness);
        await _politicalBusinessRepo.Query()
            .Where(x => x.Id == simplePoliticalBusiness.Id && x.BusinessSubType != simplePoliticalBusiness.BusinessSubType)
            .ExecuteUpdateAsync(x => x.SetProperty(prop => prop.BusinessSubType, simplePoliticalBusiness.BusinessSubType));
    }

    public async Task Delete(TPoliticalBusiness politicalBusiness) => await _politicalBusinessRepo.DeleteByKey(politicalBusiness.Id);
}
