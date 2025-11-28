// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class MajorityElectionActiveBallotGroupCreateException : Exception
{
    public MajorityElectionActiveBallotGroupCreateException(Guid primaryElectionId)
        : base($"Cannot create a ballot group if any election in the primary election {primaryElectionId} is already active")
    {
    }
}
