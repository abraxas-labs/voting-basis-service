// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Domain;

public class ComparisonVotingChannelConfiguration
{
    public VotingChannel VotingChannel { get; private set; }

    public decimal? ThresholdPercent { get; private set; }
}
