// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class PoliticalBusinessEVotingApprovedException : Exception
{
    public PoliticalBusinessEVotingApprovedException()
        : base("Political business has approved e-voting and cannot be modified")
    {
    }
}
