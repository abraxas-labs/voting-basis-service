﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ech0157_4_0;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Mapping;
using Voting.Lib.Ech;

namespace Voting.Basis.Ech.Converters;

public class Ech0157Serializer
{
    public const string EchNumber = "0157";
    public const string EchVersion = "4";

    private readonly DeliveryHeaderProvider _deliveryHeaderProvider;
    private readonly EchSerializer _echSerializer;

    public Ech0157Serializer(DeliveryHeaderProvider deliveryHeaderProvider, EchSerializer echSerializer)
    {
        _deliveryHeaderProvider = deliveryHeaderProvider;
        _echSerializer = echSerializer;
    }

    /// <summary>
    /// Serialize to eCH-0157.
    /// </summary>
    /// <param name="contest">The contest to serialize.</param>
    /// <param name="majorityElection">The majority election to serialize. It should contain the secondary elections and candidates.</param>
    /// <returns>The serialized eCH-0157 data.</returns>
    public byte[] ToDelivery(Contest contest, MajorityElection majorityElection)
        => ToDelivery(contest, new[] { majorityElection });

    /// <summary>
    /// Serialize to eCH-0157.
    /// </summary>
    /// <param name="contest">The contest to serialize.</param>
    /// <param name="majorityElections">The majority elections to serialize. They should contain the secondary elections and candidates.</param>
    /// <returns>The serialized eCH-0157 data.</returns>
    public byte[] ToDelivery(Contest contest, IEnumerable<MajorityElection> majorityElections)
    {
        var contestType = contest.ToEchContestType();
        var electionGroups = majorityElections
            .OrderBy(e => e.PoliticalBusinessNumber)
            .Select(m => m.ToEchElectionGroup())
            .ToList();

        var delivery = WrapInDelivery(
            new EventInitialDelivery
            {
                Contest = contestType,
                ElectionGroupBallot = electionGroups,
            },
            contest);

        return ToXmlBytes(delivery);
    }

    /// <summary>
    /// Serialize to eCH-0157.
    /// </summary>
    /// <param name="contest">The contest to serialize.</param>
    /// <param name="proportionalElection">The proportional election to serialize. It should contain the lists, list unions and candidates.</param>
    /// <returns>The serialized eCH-0157 data.</returns>
    public byte[] ToDelivery(Contest contest, ProportionalElection proportionalElection)
        => ToDelivery(contest, new[] { proportionalElection });

    /// <summary>
    /// Serialize to eCH-0157.
    /// </summary>
    /// <param name="contest">The contest to serialize.</param>
    /// <param name="proportionalElections">The proportional elections to serialize. They should contain the lists, list unions and candidates.</param>
    /// <returns>The serialized eCH-0157 data.</returns>
    public byte[] ToDelivery(Contest contest, IEnumerable<ProportionalElection> proportionalElections)
    {
        var contestType = contest.ToEchContestType();
        var electionGroups = proportionalElections
            .OrderBy(e => e.PoliticalBusinessNumber)
            .Select(p => p.ToEchElectionGroup())
            .ToList();

        var delivery = WrapInDelivery(
            new EventInitialDelivery
            {
                Contest = contestType,
                ElectionGroupBallot = electionGroups,
            },
            contest);

        return ToXmlBytes(delivery);
    }

    private Delivery WrapInDelivery(EventInitialDelivery data, Contest contest)
        => new Delivery
        {
            DeliveryHeader = _deliveryHeaderProvider.BuildHeader(!contest.TestingPhaseEnded),
            InitialDelivery = data,
        };

    private byte[] ToXmlBytes(Delivery delivery)
    {
        using var memoryStream = new MemoryStream();
        _echSerializer.WriteXml(memoryStream, delivery);
        return memoryStream.ToArray();
    }
}
