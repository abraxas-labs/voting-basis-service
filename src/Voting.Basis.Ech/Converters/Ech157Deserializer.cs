// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using eCH_0157_4_0;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Mapping;

namespace Voting.Basis.Ech.Converters;

public class Ech157Deserializer
{
    /// <summary>
    /// Deserialize from eCH-0157.
    /// </summary>
    /// <param name="delivery">The serialized data.</param>
    /// <returns>The deserialized eCH-0157 contest.</returns>
    public Contest FromEventInitialDelivery(DeliveryType delivery)
    {
        var idLookup = new IdLookup();
        var contest = delivery.InitialDelivery.Contest.ToBasisContest(idLookup);

        contest.ProportionalElections = delivery.InitialDelivery.ElectionGroupBallot
            .SelectMany(x => x.ElectionInformation)
            .Where(x => x.Election.TypeOfElection == eCH_0155_4_0.TypeOfElectionType.Proporz)
            .Select(x => x.ToBasisProportionalElection(idLookup))
            .ToList();

        contest.MajorityElections = delivery.InitialDelivery.ElectionGroupBallot
            .SelectMany(x => x.ElectionInformation)
            .Where(x => x.Election.TypeOfElection == eCH_0155_4_0.TypeOfElectionType.Majorz)
            .Select(x => x.ToBasisMajorityElection(idLookup))
            .ToList();

        return contest;
    }
}
