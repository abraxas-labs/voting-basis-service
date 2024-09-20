// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class ProportionalElectionEditMandateAlgorithmInUnionException : Exception
{
    public ProportionalElectionEditMandateAlgorithmInUnionException()
        : base("The mandate algorithm may only be changed in the case of proportional elections without unions.")
    {
    }
}
