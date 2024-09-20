// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class MajorityElectionCandidateIsInBallotGroupException : Exception
{
    internal MajorityElectionCandidateIsInBallotGroupException(Guid candidateId)
        : base($"Candidate {candidateId} is in a ballot group")
    {
    }
}
