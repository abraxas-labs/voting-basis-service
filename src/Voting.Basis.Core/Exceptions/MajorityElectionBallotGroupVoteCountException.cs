// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class MajorityElectionBallotGroupVoteCountException : Exception
{
    public MajorityElectionBallotGroupVoteCountException(Guid ballotGroupId)
        : base($"Election group {ballotGroupId} has an invalid entry (number of mandates != blank row count + individual candidates vote count)")
    {
    }
}
