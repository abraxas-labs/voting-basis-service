// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Core.Messaging.Messages;

public class BaseEntityMessage<TEntity>
    where TEntity : BaseEntity
{
    private TEntity? _data;

    public BaseEntityMessage(TEntity data, EntityState newEntityState)
    {
        _data = data;
        NewEntityState = newEntityState;
    }

    public TEntity? Data
    {
        get => _data;
        set
        {
            if (value == null)
            {
                _data = value;
                return;
            }

            if (_data != null && value.Id != _data.Id)
            {
                throw new InvalidOperationException("cannot set data with different id");
            }

            _data = value;
        }
    }

    public EntityState NewEntityState { get; }
}
