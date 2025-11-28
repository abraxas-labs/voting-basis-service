// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class PoliticalBusinessNotCompleteException : Exception
{
    public PoliticalBusinessNotCompleteException(string message)
    : base(message)
    {
    }
}
