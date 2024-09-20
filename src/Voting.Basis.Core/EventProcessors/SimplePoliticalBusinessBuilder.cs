// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Messaging.Extensions;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Messaging;
using EntityState = Voting.Basis.Core.Messaging.Messages.EntityState;

namespace Voting.Basis.Core.EventProcessors;

public class SimplePoliticalBusinessBuilder<TPoliticalBusiness>
    where TPoliticalBusiness : PoliticalBusiness
{
    private readonly IDbRepository<DataContext, SimplePoliticalBusiness> _politicalBusinessRepo;
    private readonly IMapper _mapper;
    private readonly MessageProducerBuffer _messageProducerBuffer;

    public SimplePoliticalBusinessBuilder(
        IDbRepository<DataContext, SimplePoliticalBusiness> politicalBusinessRepo,
        IMapper mapper,
        MessageProducerBuffer messageProducerBuffer)
    {
        _politicalBusinessRepo = politicalBusinessRepo;
        _mapper = mapper;
        _messageProducerBuffer = messageProducerBuffer;
    }

    public async Task Create(TPoliticalBusiness politicalBusiness)
    {
        var simplePoliticalBusiness = _mapper.Map<SimplePoliticalBusiness>(politicalBusiness);
        await _politicalBusinessRepo.Create(simplePoliticalBusiness);
        PublishContestDetailsChangeMessage(politicalBusiness, EntityState.Added);
    }

    public async Task Update(TPoliticalBusiness politicalBusiness)
    {
        var simplePoliticalBusiness = _mapper.Map<SimplePoliticalBusiness>(politicalBusiness);
        await _politicalBusinessRepo.Update(simplePoliticalBusiness);
        PublishContestDetailsChangeMessage(politicalBusiness, EntityState.Modified);
    }

    public async Task UpdateSubTypeIfNecessary(TPoliticalBusiness politicalBusiness)
    {
        var simplePoliticalBusiness = _mapper.Map<SimplePoliticalBusiness>(politicalBusiness);
        var affectedRows = await _politicalBusinessRepo.Query()
            .Where(x => x.Id == simplePoliticalBusiness.Id && x.BusinessSubType != simplePoliticalBusiness.BusinessSubType)
            .ExecuteUpdateAsync(x => x.SetProperty(prop => prop.BusinessSubType, simplePoliticalBusiness.BusinessSubType));

        if (affectedRows > 0)
        {
            PublishContestDetailsChangeMessage(politicalBusiness, EntityState.Modified);
        }
    }

    public async Task Delete(TPoliticalBusiness politicalBusiness)
    {
        await _politicalBusinessRepo.DeleteByKey(politicalBusiness.Id);
        PublishContestDetailsChangeMessage(politicalBusiness, EntityState.Deleted);
    }

    private void PublishContestDetailsChangeMessage(
        TPoliticalBusiness politicalBusiness,
        EntityState state)
    {
        _messageProducerBuffer.Add(new ContestDetailsChangeMessage(politicalBusiness: politicalBusiness.CreatePoliticalBusinessMessage(state)));
    }
}
