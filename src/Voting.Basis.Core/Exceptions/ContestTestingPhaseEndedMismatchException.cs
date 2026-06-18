// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class ContestTestingPhaseEndedMismatchException : Exception
{
    public ContestTestingPhaseEndedMismatchException(Guid contestId, Guid politicalBusinessId)
        : base($"Contest {contestId} testing phase ended and political business {politicalBusinessId} testing phase ended mismatch")
    {
    }
}
