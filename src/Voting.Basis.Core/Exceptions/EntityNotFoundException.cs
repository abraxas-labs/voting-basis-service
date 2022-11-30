// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class EntityNotFoundException : Exception
{
    public EntityNotFoundException(object id)
        : base($"Entity with id {id} not found")
    {
    }

    public EntityNotFoundException(string type, object id)
       : base($"{type} with id {id} not found")
    {
    }
}
