// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Messaging.Extensions;

public static class PoliticalBusinessUnionExtensions
{
    public static BaseEntityMessage<SimplePoliticalBusinessUnion> CreateBaseEntityEvent<TPoliticalBusinessUnion>(this TPoliticalBusinessUnion pbUnion, EntityState entityState, List<Guid>? politicalBusinessIds = null)
        where TPoliticalBusinessUnion : PoliticalBusinessUnion
    {
        return new BaseEntityMessage<SimplePoliticalBusinessUnion>(
            new()
            {
                Id = pbUnion.Id,
                ContestId = pbUnion.ContestId,
                UnionType = pbUnion.Type,
                PoliticalBusinessIds = politicalBusinessIds ?? new(),
            },
            entityState);
    }
}
