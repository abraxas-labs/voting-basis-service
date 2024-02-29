// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public class Ballot : BaseEntity
{
    public int Position { get; set; }

    public BallotType BallotType { get; set; }

    public bool HasTieBreakQuestions { get; set; }

    public Guid VoteId { get; set; }

    public Vote Vote { get; set; } = null!; // set by ef

    public ICollection<BallotQuestion> BallotQuestions { get; set; } = new HashSet<BallotQuestion>();

    public ICollection<TieBreakQuestion> TieBreakQuestions { get; set; } = new HashSet<TieBreakQuestion>();
}
