// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Basis.Core.Domain;

public class MajorityElectionBallotGroupCandidates
{
    public Guid BallotGroupId { get; set; }

    public List<MajorityElectionBallotGroupEntryCandidates> EntryCandidates { get; private set; } = new();
}
