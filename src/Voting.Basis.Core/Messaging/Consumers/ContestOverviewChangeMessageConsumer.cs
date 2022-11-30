// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Messaging;
using EntityState = Voting.Basis.Core.Messaging.Messages.EntityState;

namespace Voting.Basis.Core.Messaging.Consumers;

public class ContestOverviewChangeMessageConsumer : MessageConsumer<ContestOverviewChangeMessage>
{
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly ILogger<ContestOverviewChangeMessageConsumer> _logger;

    public ContestOverviewChangeMessageConsumer(
        MessageConsumerHub<ContestOverviewChangeMessage> hub,
        IDbRepository<DataContext, Contest> contestRepo,
        ILogger<ContestOverviewChangeMessageConsumer> logger)
        : base(hub)
    {
        _contestRepo = contestRepo;
        _logger = logger;
    }

    protected override async Task<ContestOverviewChangeMessage?> Transform(ContestOverviewChangeMessage message)
    {
        var contest = message.Contest.Data!;
        var contestId = contest.Id;

        if (message.Contest.NewEntityState == EntityState.Deleted)
        {
            return message;
        }

        contest = await _contestRepo.Query()
            .Include(c => c.DomainOfInfluence)
            .FirstOrDefaultAsync(c => c.Id == contest.Id);

        if (contest != null)
        {
            message.Contest.Data = contest;
            return message;
        }

        _logger.LogWarning("received contest overview changed message but could not find the contest with id {id}", contestId);
        return null!;
    }
}
