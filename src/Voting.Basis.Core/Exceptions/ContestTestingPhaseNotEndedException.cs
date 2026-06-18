// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class ContestTestingPhaseNotEndedException : Exception
{
    public ContestTestingPhaseNotEndedException()
        : base("Testing phase did not yet, operation not available")
    {
    }
}
