// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Domain;

public class ProportionalElectionCandidate
{
    public ProportionalElectionCandidate()
    {
        Number = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        PoliticalFirstName = string.Empty;
        PoliticalLastName = string.Empty;
        Occupation = new Dictionary<string, string>();
        Title = string.Empty;
        OccupationTitle = new Dictionary<string, string>();
        ZipCode = string.Empty;
        Locality = string.Empty;
    }

    public Guid Id { get; internal set; }

    public string Number { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string PoliticalFirstName { get; private set; }

    public string PoliticalLastName { get; private set; }

    public DateTime DateOfBirth { get; private set; }

    public SexType Sex { get; private set; }

    public Dictionary<string, string> Occupation { get; private set; }

    public string Title { get; private set; }

    public Dictionary<string, string> OccupationTitle { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the candidate is incumbent (in german: Bisher).
    /// </summary>
    public bool Incumbent { get; private set; }

    public string ZipCode { get; private set; }

    public string Locality { get; private set; }

    public int Position { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether this candidate is accumulated (in german: Kumuliert).
    /// This means that the candidate exists twice on the list.
    /// </summary>
    public bool Accumulated { get; private set; }

    /// <summary>
    /// Gets the accumulated position if the candidate is <see cref="Accumulated"/>.
    /// </summary>
    public int AccumulatedPosition { get; internal set; }

    public Guid? PartyId { get; private set; }

    public Guid ProportionalElectionListId { get; set; }
}
