// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;
public class MajorityElectionActiveNumberOfMandatesChangeException : Exception
{
    public MajorityElectionActiveNumberOfMandatesChangeException(Guid electionId)
        : base($"Cannot update the number of mandates on active election {electionId}")
    {
    }
}
