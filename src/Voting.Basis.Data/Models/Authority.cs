// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Data.Models;

public class Authority : BaseAuthority
{
    public CountingCircle? CountingCircle { get; set; }

    public Guid CountingCircleId { get; set; }
}
