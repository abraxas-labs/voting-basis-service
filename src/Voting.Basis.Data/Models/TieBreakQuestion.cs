// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public class TieBreakQuestion : BaseEntity
{
    public int Number { get; set; }

    public Dictionary<string, string> Question { get; set; } = new Dictionary<string, string>();

    public int Question1Number { get; set; }

    public int Question2Number { get; set; }

    public Ballot Ballot { get; set; } = null!;

    public Guid BallotId { get; set; }
}
