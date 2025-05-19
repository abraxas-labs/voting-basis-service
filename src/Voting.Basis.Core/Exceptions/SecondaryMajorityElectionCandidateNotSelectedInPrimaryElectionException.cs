// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class SecondaryMajorityElectionCandidateNotSelectedInPrimaryElectionException : Exception
{
    public SecondaryMajorityElectionCandidateNotSelectedInPrimaryElectionException()
        : base(
            "Cannot select a referenced candidate in a secondary election if the candidate is not selected in the primary election")
    {
    }
}
