// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Domain;

public class ComparisonVoterParticipationConfiguration
{
    public DomainOfInfluenceType MainLevel { get; private set; }

    public DomainOfInfluenceType ComparisonLevel { get; private set; }

    public decimal? ThresholdPercent { get; private set; }
}
