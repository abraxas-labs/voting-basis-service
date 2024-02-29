// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class ContestTestingPhaseEndedException : Exception
{
    public ContestTestingPhaseEndedException()
        : base("Testing phase ended, cannot modify the contest")
    {
    }
}
