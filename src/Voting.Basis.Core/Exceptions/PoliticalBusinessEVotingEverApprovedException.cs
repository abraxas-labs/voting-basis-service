// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class PoliticalBusinessEVotingEverApprovedException : Exception
{
    public PoliticalBusinessEVotingEverApprovedException()
    : base("Political business has approved e-voting once.")
    {
    }
}
