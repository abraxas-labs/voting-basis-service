// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public class PlausibilisationConfiguration : BaseEntity
{
    public DomainOfInfluence DomainOfInfluence { get; set; } = null!;

    public Guid DomainOfInfluenceId { get; set; }

    public ICollection<ComparisonVoterParticipationConfiguration> ComparisonVoterParticipationConfigurations { get; set; }
        = new HashSet<ComparisonVoterParticipationConfiguration>();

    public ICollection<ComparisonVotingChannelConfiguration> ComparisonVotingChannelConfigurations { get; set; }
        = new HashSet<ComparisonVotingChannelConfiguration>();

    public ICollection<ComparisonCountOfVotersConfiguration> ComparisonCountOfVotersConfigurations { get; set; }
        = new HashSet<ComparisonCountOfVotersConfiguration>();

    public decimal? ComparisonValidVotingCardsWithAccountedBallotsThresholdPercent { get; set; }
}
