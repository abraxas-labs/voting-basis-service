// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Core.Messaging.Messages;

public static class BaseEntityMessageExtensions
{
    public static bool HasEqualIdAndNewEntityState<TEntity>(this BaseEntityMessage<TEntity>? message, Guid id, EntityState entityState)
        where TEntity : BaseEntity
    {
        return message != null && message.Data?.Id == id && message.NewEntityState == entityState;
    }
}
