// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Messaging.Extensions;

public static class PoliticalBusinessUnionExtensions
{
    public static BaseEntityMessage<SimplePoliticalBusinessUnion> CreateBaseEntityEvent<TPoliticalBusinessUnion>(this TPoliticalBusinessUnion pbUnion, EntityState entityState)
        where TPoliticalBusinessUnion : PoliticalBusinessUnion
    {
        return new BaseEntityMessage<SimplePoliticalBusinessUnion>(
            new()
            {
                Id = pbUnion.Id,
                ContestId = pbUnion.ContestId,
                UnionType = pbUnion.Type,
            },
            entityState);
    }
}
