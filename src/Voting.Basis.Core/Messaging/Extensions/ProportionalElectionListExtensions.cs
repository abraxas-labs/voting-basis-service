// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Messaging.Extensions;

public static class ProportionalElectionListExtensions
{
    public static BaseEntityMessage<ProportionalElectionList> CreateBaseEntityEvent(this ProportionalElectionList list, EntityState entityState)
    {
        return new BaseEntityMessage<ProportionalElectionList>(
            new()
            {
                Id = list.Id,
                ProportionalElection = list.ProportionalElection,
            },
            entityState);
    }
}
