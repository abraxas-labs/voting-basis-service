﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Domain;

public class MajorityElectionCandidate
{
    public MajorityElectionCandidate()
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
        Party = new Dictionary<string, string>();
        Origin = string.Empty;
        Country = string.Empty;
        Street = string.Empty;
        HouseNumber = string.Empty;
    }

    public Guid Id { get; internal set; }

    public string Number { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string PoliticalFirstName { get; private set; }

    public string PoliticalLastName { get; private set; }

    public DateTime? DateOfBirth { get; private set; }

    public SexType Sex { get; private set; }

    public Dictionary<string, string> Occupation { get; private set; }

    public string Title { get; private set; }

    public Dictionary<string, string> OccupationTitle { get; private set; }

    public Dictionary<string, string> Party { get; set; }

    /// <summary>
    /// Gets a value indicating whether the candidate is incumbent (in german: Bisher).
    /// </summary>
    public bool Incumbent { get; private set; }

    public string ZipCode { get; private set; }

    public string Locality { get; private set; }

    public int Position { get; internal set; }

    public Guid MajorityElectionId { get; set; }

    public string Origin { get; private set; }

    public int CheckDigit { get; internal set; }

    public string Country { get; private set; }

    public string Street { get; private set; }

    public string HouseNumber { get; private set; }
}
