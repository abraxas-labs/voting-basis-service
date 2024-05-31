// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Voting.Basis.Core.Import;

public class IdVerifier
{
    private readonly HashSet<Guid> _uniqueGuids = new();

    public void EnsureUnique(Guid id)
    {
        if (!_uniqueGuids.Add(id))
        {
            throw new ValidationException("This id is not unique");
        }
    }
}
