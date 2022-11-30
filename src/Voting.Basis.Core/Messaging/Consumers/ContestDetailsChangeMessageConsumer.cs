// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Messaging;
using EntityState = Voting.Basis.Core.Messaging.Messages.EntityState;

namespace Voting.Basis.Core.Messaging.Consumers;

public class ContestDetailsChangeMessageConsumer : MessageConsumer<ContestDetailsChangeMessage>
{
    private readonly IDbRepository<DataContext, SimplePoliticalBusiness> _simplePbRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionUnion> _proportionalElectionUnionRepo;
    private readonly IDbRepository<DataContext, MajorityElectionUnion> _majorityElectionUnionRepo;
    private readonly IDbRepository<DataContext, ElectionGroup> _electionGroupRepo;
    private readonly ILogger<ContestDetailsChangeMessageConsumer> _logger;
    private readonly IMapper _mapper;

    public ContestDetailsChangeMessageConsumer(
        MessageConsumerHub<ContestDetailsChangeMessage> hub,
        IDbRepository<DataContext, SimplePoliticalBusiness> simplePbRepo,
        IDbRepository<DataContext, ProportionalElectionUnion> proportionalElectionUnionRepo,
        IDbRepository<DataContext, MajorityElectionUnion> majorityElectionUnionRepo,
        IDbRepository<DataContext, ElectionGroup> electionGroupRepo,
        ILogger<ContestDetailsChangeMessageConsumer> logger,
        IMapper mapper)
        : base(hub)
    {
        _simplePbRepo = simplePbRepo;
        _proportionalElectionUnionRepo = proportionalElectionUnionRepo;
        _majorityElectionUnionRepo = majorityElectionUnionRepo;
        _electionGroupRepo = electionGroupRepo;
        _logger = logger;
        _mapper = mapper;
    }

    protected override async Task<ContestDetailsChangeMessage?> Transform(ContestDetailsChangeMessage message)
    {
        if (message.PoliticalBusinessUnion != null)
        {
            await TransformPoliticalBusinessUnionEvent(message);
            return message;
        }

        if (message.PoliticalBusiness != null)
        {
            await TransformPoliticalBusinessEvent(message);
            return message;
        }

        await TransformElectionGroupEvent(message);
        return message;
    }

    private async Task TransformPoliticalBusinessUnionEvent(ContestDetailsChangeMessage message)
    {
        var isProportionalElectionUnion = message.PoliticalBusinessUnion!.Data!.Type == PoliticalBusinessUnionType.ProportionalElection;
        var messagePbUnion = message.PoliticalBusinessUnion.Data;

        if (message.PoliticalBusinessUnion.NewEntityState == EntityState.Deleted)
        {
            return;
        }

        var existingPbUnion = isProportionalElectionUnion
            ? _mapper.Map<SimplePoliticalBusinessUnion>(await _proportionalElectionUnionRepo.GetByKey(messagePbUnion.Id))
            : _mapper.Map<SimplePoliticalBusinessUnion>(await _majorityElectionUnionRepo.GetByKey(messagePbUnion.Id));

        if (existingPbUnion != null)
        {
            message.PoliticalBusinessUnion.Data = existingPbUnion;
            return;
        }

        message.PoliticalBusinessUnion.Data = null;
        _logger.LogWarning("received contest details changed message but could not find the political bussiness union with id {Id}", messagePbUnion);
    }

    private async Task TransformPoliticalBusinessEvent(ContestDetailsChangeMessage message)
    {
        var messagePb = message.PoliticalBusiness!.Data!;

        if (message.PoliticalBusiness.NewEntityState == EntityState.Deleted)
        {
            return;
        }

        var existingPb = await _simplePbRepo.Query()
            .Include(x => x.DomainOfInfluence)
            .FirstOrDefaultAsync(x => x.Id == messagePb.Id);

        if (existingPb != null)
        {
            message.PoliticalBusiness.Data = existingPb;
            return;
        }

        message.PoliticalBusiness.Data = null;
        _logger.LogWarning("received contest details changed message but could not find the political bussiness with id {id}", messagePb);
    }

    private async Task TransformElectionGroupEvent(ContestDetailsChangeMessage message)
    {
        var messageElectionGroup = message.ElectionGroup!.Data!;

        if (message.ElectionGroup.NewEntityState == EntityState.Deleted)
        {
            return;
        }

        var existingElectionGroup = await _electionGroupRepo.Query()
            .Include(e => e.PrimaryMajorityElection)
            .Include(e => e.SecondaryMajorityElections)
            .FirstOrDefaultAsync(e => e.Id == messageElectionGroup.Id);

        if (existingElectionGroup != null)
        {
            message.ElectionGroup.Data = existingElectionGroup;
            return;
        }

        message.ElectionGroup.Data = null!;
        _logger.LogWarning("received contest details changed message but could not find election group with id {id}", messageElectionGroup);
    }
}
