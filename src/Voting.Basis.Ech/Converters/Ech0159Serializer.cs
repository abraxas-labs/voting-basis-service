// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ech0159_4_0;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Mapping;
using Voting.Lib.Ech;

namespace Voting.Basis.Ech.Converters;

public class Ech0159Serializer
{
    public const string EchNumber = "0159";
    public const string EchVersion = "4";

    private readonly DeliveryHeaderProvider _deliveryHeaderProvider;
    private readonly EchSerializer _echSerializer;

    public Ech0159Serializer(DeliveryHeaderProvider deliveryHeaderProvider, EchSerializer echSerializer)
    {
        _deliveryHeaderProvider = deliveryHeaderProvider;
        _echSerializer = echSerializer;
    }

    /// <summary>
    /// Serialize to eCH-0159.
    /// </summary>
    /// <param name="contest">The contest to serialize.</param>
    /// <param name="vote">The vote to serialize. It should contain the ballots, questions and tie break questions.</param>
    /// <returns>The serialized eCH-0159 data.</returns>
    public byte[] ToEventInitialDelivery(Contest contest, Vote vote)
        => ToEventInitialDelivery(contest, new[] { vote });

    /// <summary>
    /// Serialize to ECH-159.
    /// </summary>
    /// <param name="contest">The contest to serialize.</param>
    /// <param name="votes">The votes to serialize. They should contain the ballots, questions and tie break questions.</param>
    /// <returns>The serialized eCH-0159 data.</returns>
    public byte[] ToEventInitialDelivery(Contest contest, IEnumerable<Vote> votes)
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
            .ToList();

        return ToXmlBytes(new Delivery
        {
            DeliveryHeader = _deliveryHeaderProvider.BuildHeader(!contest.TestingPhaseEnded),
            InitialDelivery = new EventInitialDelivery
            {
                Contest = contestType,
                VoteInformation = voteTypes,
            },
        });
    }

    private byte[] ToXmlBytes(Delivery delivery)
    {
        using var memoryStream = new MemoryStream();
        _echSerializer.WriteXml(memoryStream, delivery);
        return memoryStream.ToArray();
    }
}
