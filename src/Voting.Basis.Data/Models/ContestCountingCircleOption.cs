// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public class ContestCountingCircleOption : BaseEntity
{
    public Contest? Contest { get; set; }

    public Guid ContestId { get; set; }

    public CountingCircle? CountingCircle { get; set; }

    public Guid CountingCircleId { get; set; }

    public bool EVoting { get; set; }
}
