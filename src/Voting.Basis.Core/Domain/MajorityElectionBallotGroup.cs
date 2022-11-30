// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Basis.Core.Domain;

public class MajorityElectionBallotGroup
{
    public MajorityElectionBallotGroup()
    {
        ShortDescription = string.Empty;
        Description = string.Empty;
        Entries = new List<MajorityElectionBallotGroupEntry>();
    }

    public Guid Id { get; internal set; }

    public Guid MajorityElectionId { get; set; }

    public string ShortDescription { get; private set; }

    public string Description { get; private set; }

    public int Position { get; internal set; }

    /// <summary>
    /// Gets the entries of this ballot group. One entry per election (only has multiple entries if secondary elections exist).
    /// </summary>
    public List<MajorityElectionBallotGroupEntry> Entries { get; private set; }
}
