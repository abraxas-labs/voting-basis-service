// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Messaging.Extensions;

public static class ElectionGroupExtensions
{
    public static BaseEntityMessage<ElectionGroup> CreateBaseEntityEvent(this ElectionGroup electionGroup, EntityState entityState)
    {
        return new BaseEntityMessage<ElectionGroup>(
            new()
            {
                Id = electionGroup.Id,
                PrimaryMajorityElectionId = electionGroup.PrimaryMajorityElectionId,
                PrimaryMajorityElection = new()
                {
                    Id = electionGroup.PrimaryMajorityElectionId,
                    DomainOfInfluenceId = electionGroup.PrimaryMajorityElection.DomainOfInfluenceId,
                    ContestId = electionGroup.PrimaryMajorityElection.ContestId,
                },
            },
            entityState);
    }
}
