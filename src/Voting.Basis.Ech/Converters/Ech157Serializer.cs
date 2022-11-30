// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using eCH_0157_4_0;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Mapping;
using Voting.Lib.Ech;

namespace Voting.Basis.Ech.Converters;

public class Ech157Serializer
{
    private readonly DeliveryHeaderProvider _deliveryHeaderProvider;

    public Ech157Serializer(DeliveryHeaderProvider deliveryHeaderProvider)
    {
        _deliveryHeaderProvider = deliveryHeaderProvider;
    }

    /// <summary>
    /// Serialize to ECH-157.
    /// </summary>
    /// <param name="contest">The contest to serialize.</param>
    /// <param name="majorityElection">The majority election to serialize. It should contain the secondary elections and candidates.</param>
    /// <returns>The serialized ECH-157 data.</returns>
    public DeliveryType ToDelivery(Contest contest, MajorityElection majorityElection)
        => ToDelivery(contest, new[] { majorityElection });

    /// <summary>
    /// Serialize to ECH-157.
    /// </summary>
    /// <param name="contest">The contest to serialize.</param>
    /// <param name="majorityElections">The majority elections to serialize. They should contain the secondary elections and candidates.</param>
    /// <returns>The serialized ECH-157 data.</returns>
    public DeliveryType ToDelivery(Contest contest, IEnumerable<MajorityElection> majorityElections)
    {
        var contestType = contest.ToEchContestType();
        var electionGroups = majorityElections
            .OrderBy(e => e.PoliticalBusinessNumber)
            .Select(m => m.ToEchElectionGroup())
            .ToArray();

        return WrapInDelivery(EventInitialDeliveryType.Create(contestType, electionGroups));
    }

    /// <summary>
    /// Serialize to ECH-157.
    /// </summary>
    /// <param name="contest">The contest to serialize.</param>
    /// <param name="proportionalElection">The proportional election to serialize. It should contain the lists, list unions and candidates.</param>
    /// <returns>The serialized ECH-157 data.</returns>
    public DeliveryType ToDelivery(Contest contest, ProportionalElection proportionalElection)
        => ToDelivery(contest, new[] { proportionalElection });

    /// <summary>
    /// Serialize to ECH-157.
    /// </summary>
    /// <param name="contest">The contest to serialize.</param>
    /// <param name="proportionalElections">The proportional elections to serialize. They should contain the lists, list unions and candidates.</param>
    /// <returns>The serialized ECH-157 data.</returns>
    public DeliveryType ToDelivery(Contest contest, IEnumerable<ProportionalElection> proportionalElections)
    {
        var contestType = contest.ToEchContestType();
        var electionGroups = proportionalElections
            .OrderBy(e => e.PoliticalBusinessNumber)
            .Select(p => p.ToEchElectionGroup())
            .ToArray();

        return WrapInDelivery(EventInitialDeliveryType.Create(contestType, electionGroups));
    }

    private DeliveryType WrapInDelivery(EventInitialDeliveryType data)
        => DeliveryType.Create(_deliveryHeaderProvider.BuildHeader(), data);
}
