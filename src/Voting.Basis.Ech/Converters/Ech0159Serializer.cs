﻿// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using eCH_0159_4_0;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Mapping;
using Voting.Lib.Ech;

namespace Voting.Basis.Ech.Converters;

public class Ech0159Serializer
{
    private readonly DeliveryHeaderProvider _deliveryHeaderProvider;

    public Ech0159Serializer(DeliveryHeaderProvider deliveryHeaderProvider)
    {
        _deliveryHeaderProvider = deliveryHeaderProvider;
    }

    /// <summary>
    /// Serialize to eCH-0159.
    /// </summary>
    /// <param name="contest">The contest to serialize.</param>
    /// <param name="vote">The vote to serialize. It should contain the ballots, questions and tie break questions.</param>
    /// <returns>The serialized eCH-0159 data.</returns>
    public Delivery ToEventInitialDelivery(Contest contest, Vote vote)
        => ToEventInitialDelivery(contest, new[] { vote });

    /// <summary>
    /// Serialize to ECH-159.
    /// </summary>
    /// <param name="contest">The contest to serialize.</param>
    /// <param name="votes">The votes to serialize. They should contain the ballots, questions and tie break questions.</param>
    /// <returns>The serialized eCH-0159 data.</returns>
    public Delivery ToEventInitialDelivery(Contest contest, IEnumerable<Vote> votes)
    {
        var contestType = contest.ToEchContestType();

        // "VOTING votes" are grouped by their respective domain of influence
        // A vote from this VOTING system represents a ballot inside the eCH vote.
        var voteTypes = votes
            .Where(v => v.Ballots.Count > 0) // The standard requires at least one ballot
            .GroupBy(v => v.DomainOfInfluenceId)
            .Select(x => x.ToEchVoteInformation())
            .OrderBy(x => x.DoiType)
            .ThenBy(x => x.VoteInformation.Vote.DomainOfInfluenceIdentification)
            .Select(x => x.VoteInformation)
            .ToArray();

        return Delivery.Create(_deliveryHeaderProvider.BuildHeader(), EventInitialDelivery.Create(contestType, voteTypes));
    }
}
