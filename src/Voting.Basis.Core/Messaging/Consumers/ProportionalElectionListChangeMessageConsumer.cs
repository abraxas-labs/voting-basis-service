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

public class ProportionalElectionListChangeMessageConsumer : MessageConsumer<ProportionalElectionListChangeMessage>
{
    private readonly IDbRepository<DataContext, ProportionalElectionList> _listRepo;
    private readonly ILogger<ProportionalElectionListChangeMessageConsumer> _logger;

    public ProportionalElectionListChangeMessageConsumer(
        MessageConsumerHub<ProportionalElectionListChangeMessage> hub,
        IDbRepository<DataContext, ProportionalElectionList> listRepo,
        ILogger<ProportionalElectionListChangeMessageConsumer> logger)
        : base(hub)
    {
        _listRepo = listRepo;
        _logger = logger;
    }

    protected override async Task<ProportionalElectionListChangeMessage?> Transform(ProportionalElectionListChangeMessage message)
    {
        var list = message.List.Data!;
        var listId = list.Id;

        if (message.List.NewEntityState == EntityState.Deleted)
        {
            return message;
        }

        list = await _listRepo.Query()
            .Include(cc => cc.ProportionalElection)
            .FirstOrDefaultAsync(c => c.Id == listId);

        if (list != null)
        {
            message.List.Data = list;
            return message;
        }

        _logger.LogWarning("received proportional election list changed message but could not find the proportional election list with id {id}", listId);
        return null!;
    }
}
