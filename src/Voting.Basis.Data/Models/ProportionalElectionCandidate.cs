// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Data.Models;

public class ProportionalElectionCandidate : ElectionCandidate
{
    public bool Accumulated { get; set; }

    public int AccumulatedPosition { get; set; }

    public Guid ProportionalElectionListId { get; set; }

    public ProportionalElectionList ProportionalElectionList { get; set; } = null!;

    public Guid? PartyId { get; set; }

    public DomainOfInfluenceParty? Party { get; set; }
}
