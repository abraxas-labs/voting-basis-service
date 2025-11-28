// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class ProportionalElectionListUnionMissingListsException : Exception
{
    public ProportionalElectionListUnionMissingListsException(Guid electionId)
        : base($"Election {electionId} has list unions with less than 2 lists")
    {
    }
}
