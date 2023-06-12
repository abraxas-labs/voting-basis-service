// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Basis.Core.Domain;

public class ProportionalElectionList
{
    public ProportionalElectionList()
    {
        OrderNumber = string.Empty;
        Description = new Dictionary<string, string>();
        ShortDescription = new Dictionary<string, string>();
        Candidates = new List<ProportionalElectionCandidate>();
    }

    public Guid Id { get; internal set; }

    /// <summary>
    /// Gets the order number ("Listennummer").
    /// </summary>
    public string OrderNumber { get; private set; }

    public Dictionary<string, string> Description { get; private set; }

    public Dictionary<string, string> ShortDescription { get; private set; }

    /// <summary>
    /// Gets the count of blank/empty rows.
    /// </summary>
    public int BlankRowCount { get; private set; }

    public int Position { get; internal set; }

    public List<ProportionalElectionCandidate> Candidates { get; private set; }

    public Guid ProportionalElectionId { get; set; }

    internal void DecreasePosition()
    {
        Position--;
    }
}
