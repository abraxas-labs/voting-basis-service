// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class ProportionalElectionListIsInListUnionException : Exception
{
    public ProportionalElectionListIsInListUnionException(Guid listId)
        : base($"List {listId} is in a list union")
    {
    }
}
