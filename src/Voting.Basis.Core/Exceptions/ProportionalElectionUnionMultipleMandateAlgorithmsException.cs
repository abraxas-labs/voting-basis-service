// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class ProportionalElectionUnionMultipleMandateAlgorithmsException : Exception
{
    public ProportionalElectionUnionMultipleMandateAlgorithmsException()
        : base("Only proportional elections with the same mandate algorithms may be combined.")
    {
    }
}
