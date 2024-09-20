// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Messaging.Extensions;

public static class ContestExtensions
{
    public static BaseEntityMessage<Contest> CreateBaseEntityEvent(this Contest contest, EntityState entityState)
    {
        return new BaseEntityMessage<Contest>(
            new()
            {
                Id = contest.Id,
                DomainOfInfluenceId = contest.DomainOfInfluenceId,
                DomainOfInfluence = new() { Id = contest.DomainOfInfluenceId },
                Date = contest.Date,
                EndOfTestingPhase = contest.EndOfTestingPhase,
            },
            entityState);
    }
}
