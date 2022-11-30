// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using eCH_0159_4_0;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Mapping;

namespace Voting.Basis.Ech.Converters;

public class Ech159Deserializer
{
    /// <summary>
    /// Deserialize from eCH-0159.
    /// </summary>
    /// <param name="delivery">The serialized data.</param>
    /// <returns>The deserialized eCH-0159 contest.</returns>
    public Contest FromEventInitialDelivery(Delivery delivery)
    {
        var idLookup = new IdLookup();

        var contest = delivery.InitialDelivery.ContestType.ToBasisContest(idLookup);
        contest.Votes = delivery.InitialDelivery.VoteInformation
            .SelectMany(v => v.ToBasisVotes(idLookup))
            .ToList();

        return contest;
    }
}
