// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class ContestSetAsPreviousContestException : Exception
{
    public ContestSetAsPreviousContestException()
        : base("contest already set as a previous contest of an other contest")
    {
    }
}
