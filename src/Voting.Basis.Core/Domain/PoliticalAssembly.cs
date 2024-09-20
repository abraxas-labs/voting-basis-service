// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Basis.Core.Domain;

/// <summary>
/// A political assembly (in german: Versammlung).
/// </summary>
public class PoliticalAssembly
{
    public Guid Id { get; set; }

    public DateTime Date { get; set; }

    public Dictionary<string, string> Description { get; set; } = new();

    /// <summary>
    /// Gets or sets the domain of influence responsible for this contest.
    /// </summary>
    public Guid DomainOfInfluenceId { get; set; }
}
