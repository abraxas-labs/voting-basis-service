// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public class ComparisonVoterParticipationConfiguration : BaseEntity
{
    public DomainOfInfluenceType MainLevel { get; set; }

    public DomainOfInfluenceType ComparisonLevel { get; set; }

    public decimal? ThresholdPercent { get; set; }

    public PlausibilisationConfiguration PlausibilisationConfiguration { get; set; } = null!;

    public Guid PlausibilisationConfigurationId { get; set; }
}
