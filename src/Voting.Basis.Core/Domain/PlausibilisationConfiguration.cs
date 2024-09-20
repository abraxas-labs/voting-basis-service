// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Basis.Core.Domain;

public class PlausibilisationConfiguration
{
    public ICollection<ComparisonVoterParticipationConfiguration> ComparisonVoterParticipationConfigurations { get; private set; }
        = new HashSet<ComparisonVoterParticipationConfiguration>();

    public ICollection<ComparisonVotingChannelConfiguration> ComparisonVotingChannelConfigurations { get; private set; }
        = new HashSet<ComparisonVotingChannelConfiguration>();

    public ICollection<ComparisonCountOfVotersConfiguration> ComparisonCountOfVotersConfigurations { get; private set; }
        = new HashSet<ComparisonCountOfVotersConfiguration>();

    public decimal? ComparisonValidVotingCardsWithAccountedBallotsThresholdPercent { get; private set; }

    public IReadOnlyCollection<ComparisonCountOfVotersCountingCircleEntry> ComparisonCountOfVotersCountingCircleEntries { get; set; }
        = Array.Empty<ComparisonCountOfVotersCountingCircleEntry>();
}
