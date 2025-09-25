// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Domain;

/// <summary>
/// A contest (in german: Urnengang).
/// </summary>
public class Contest
{
    public Guid Id { get; set; }

    public DateTime Date { get; set; }

    public Dictionary<string, string> Description { get; set; } = new();

    /// <summary>
    /// Gets or sets the end of the contest testing phase (see <see cref="ContestState.TestingPhase"/>).
    /// </summary>
    public DateTime EndOfTestingPhase { get; set; }

    /// <summary>
    /// Gets or sets the domain of influence responsible for this contest.
    /// </summary>
    public Guid DomainOfInfluenceId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether E-Voting is allowed in this contest.
    /// </summary>
    public bool EVoting { get; set; }

    /// <summary>
    /// Gets or sets the date from which on entering E-Voting results is allowed.
    /// </summary>
    public DateTime? EVotingFrom { get; set; }

    /// <summary>
    /// Gets or sets the date until when entering E-Voting results is allowed.
    /// </summary>
    public DateTime? EVotingTo { get; set; }

    /// <summary>
    /// Gets or sets the due date of the e-voting approval.
    /// </summary>
    public DateTime? EVotingApprovalDueDate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the e-voting data has been approved for this contest.
    /// </summary>
    public bool EVotingApproved { get; set; }

    /// <summary>
    /// Gets or sets the optional previous contest (a contest that took place sometime earlier).
    /// The previous contest is used mainly for plausibility checks.
    /// </summary>
    public Guid? PreviousContestId { get; set; }

    public ContestState State { get; set; } = ContestState.TestingPhase;

    /// <summary>
    /// Gets or sets the list of contests that were merged into this contest.
    /// </summary>
    public HashSet<Guid> MergedContestIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the date after which the contest is past locked (see <see cref="ContestState.PastLocked"/>).
    /// </summary>
    public DateTime? PastLockPer { get; set; }

    /// <summary>
    /// Gets or sets the date after which the contest is archived (see <see cref="ContestState.Archived"/>).
    /// </summary>
    public DateTime? ArchivePer { get; set; }
}
