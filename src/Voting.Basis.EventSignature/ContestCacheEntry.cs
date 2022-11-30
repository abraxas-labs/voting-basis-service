// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.EventSignature;

public class ContestCacheEntry
{
    public Guid Id { get; set; }

    public ContestCacheEntryKeyData? KeyData { get; set; }
}
