// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Domain;

public class CantonSettingsVotingCardChannel
{
    public VotingChannel VotingChannel { get; set; }

    public bool Valid { get; set; }
}
