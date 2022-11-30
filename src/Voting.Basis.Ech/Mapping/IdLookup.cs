// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Basis.Ech.Mapping;

internal class IdLookup
{
    private readonly Dictionary<string, Guid> _idLookup = new Dictionary<string, Guid>();

    internal Guid GuidForId(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return Guid.NewGuid();
        }

        if (!_idLookup.TryGetValue(id, out var guid))
        {
            guid = Guid.NewGuid();
            _idLookup.Add(id, guid);
        }

        return guid;
    }
}
