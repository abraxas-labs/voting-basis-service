// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class ContestMissingEVotingException : Exception
{
    public ContestMissingEVotingException()
    : base("Contest has no E-Voting support")
    {
    }
}
