// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public class Ballot : BaseEntity
{
    public int Position { get; set; }

    public BallotType BallotType { get; set; }

    /// <summary>
    /// Gets or sets the sub type.
    /// Only relevant when the VoteType is VariantQuestionsOnMultipleBallots.
    /// </summary>
    public BallotSubType SubType { get; set; }

    /// <summary>
    /// Gets or sets the official description.
    /// Only relevant when the VoteType is VariantQuestionsOnMultipleBallots.
    /// </summary>
    public Dictionary<string, string> OfficialDescription { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the short description.
    /// Only relevant when the VoteType is VariantQuestionsOnMultipleBallots.
    /// </summary>
    public Dictionary<string, string> ShortDescription { get; set; } = new Dictionary<string, string>();

    public Guid VoteId { get; set; }

    public Vote Vote { get; set; } = null!; // set by ef

    public bool HasTieBreakQuestions { get; set; }

    public ICollection<BallotQuestion> BallotQuestions { get; set; } = new HashSet<BallotQuestion>();

    public ICollection<TieBreakQuestion> TieBreakQuestions { get; set; } = new HashSet<TieBreakQuestion>();
}
