// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Basis.Core.Domain.Aggregate;

public abstract class PoliticalBusinessAggregate : BaseHasContestAggregate
{
    public bool TestingPhaseEnded { get; protected set; }

    public abstract void EndTestingPhase();
}
