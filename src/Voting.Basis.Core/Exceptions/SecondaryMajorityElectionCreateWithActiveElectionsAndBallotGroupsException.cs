// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class SecondaryMajorityElectionCreateWithActiveElectionsAndBallotGroupsException : Exception
{
    public SecondaryMajorityElectionCreateWithActiveElectionsAndBallotGroupsException(Guid primaryMajorityElectionId)
    : base($"Cannot create a secondary majority election when the primary election {primaryMajorityElectionId} is already active and ballot groups exist")
    {
    }
}
