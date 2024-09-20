// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public class BallotQuestion : BaseEntity
{
    public int Number { get; set; }

    public Dictionary<string, string> Question { get; set; } = new Dictionary<string, string>();

    public Guid BallotId { get; set; }

    public Ballot Ballot { get; set; } = null!; // set by ef

    public BallotQuestionType Type { get; set; }

    public int? FederalIdentification { get; set; }
}
