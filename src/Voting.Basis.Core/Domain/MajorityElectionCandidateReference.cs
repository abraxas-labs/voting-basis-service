﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Domain;

/// <summary>
/// A candidate reference. Secondary majority elections can reference candidates from the main majority election.
/// </summary>
public class MajorityElectionCandidateReference
{
    public Guid Id { get; internal set; }

    public bool Incumbent { get; internal set; }

    public int Position { get; internal set; }

    public string Number { get; internal set; } = string.Empty;

    public Guid CandidateId { get; set; }

    public Guid SecondaryMajorityElectionId { get; set; }
}
