// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Data.Models.Snapshots;

public class AuthoritySnapshot : BaseAuthority
{
    public CountingCircleSnapshot? CountingCircle { get; set; }

    public Guid CountingCircleId { get; set; }
}
