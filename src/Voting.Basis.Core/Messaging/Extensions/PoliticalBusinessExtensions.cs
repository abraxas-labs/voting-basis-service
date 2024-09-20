// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Messaging.Extensions;

public static class PoliticalBusinessExtensions
{
    public static BaseEntityMessage<SimplePoliticalBusiness> CreatePoliticalBusinessMessage<TPoliticalBusiness>(this TPoliticalBusiness pb, EntityState entityState)
        where TPoliticalBusiness : PoliticalBusiness
    {
        return new BaseEntityMessage<SimplePoliticalBusiness>(
            new()
            {
                Id = pb.Id,
                ContestId = pb.ContestId,
                DomainOfInfluenceId = pb.DomainOfInfluenceId,
                DomainOfInfluence = new() { Id = pb.DomainOfInfluenceId },
                BusinessType = pb.PoliticalBusinessType,
                BusinessSubType = pb.PoliticalBusinessSubType,
            },
            entityState);
    }
}
