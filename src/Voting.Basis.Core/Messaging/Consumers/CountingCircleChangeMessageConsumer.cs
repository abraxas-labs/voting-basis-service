// (c) Copyright by Abraxas Informatik AG
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

public class CountingCircleChangeMessageConsumer : MessageConsumer<CountingCircleChangeMessage>
{
    private readonly IDbRepository<DataContext, CountingCircle> _countingCircleRepo;
    private readonly ILogger<CountingCircleChangeMessageConsumer> _logger;

    public CountingCircleChangeMessageConsumer(
        MessageConsumerHub<CountingCircleChangeMessage> hub,
        IDbRepository<DataContext, CountingCircle> countingCircleRepo,
        ILogger<CountingCircleChangeMessageConsumer> logger)
        : base(hub)
    {
        _countingCircleRepo = countingCircleRepo;
        _logger = logger;
    }

    protected override async Task<CountingCircleChangeMessage?> Transform(CountingCircleChangeMessage message)
    {
        var countingCircle = message.CountingCircle.Data!;
        var countingCircleId = countingCircle.Id;

        if (message.CountingCircle.NewEntityState == EntityState.Deleted)
        {
            return message;
        }

        countingCircle = await _countingCircleRepo.Query()
            .Include(cc => cc.ResponsibleAuthority)
            .FirstOrDefaultAsync(c => c.Id == countingCircleId);

        if (countingCircle != null)
        {
            message.CountingCircle.Data = countingCircle;
            return message;
        }

        _logger.LogWarning("received counting circle changed message but could not find the counting circle with id {id}", countingCircleId);
        return null!;
    }
}
