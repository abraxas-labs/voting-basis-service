// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using eCH_0159_4_0;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Mapping;
using Voting.Basis.Ech.Schemas;

namespace Voting.Basis.Ech.Converters;

public static class Ech0159Deserializer
{
    /// <summary>
    /// Deserialize from eCH-0159.
    /// </summary>
    /// <param name="xml">The serialized XML data.</param>
    /// <returns>The deserialized eCH-0159 contest.</returns>
    public static Contest DeserializeXml(string xml)
    {
        var schemaSet = Ech0159SchemaLoader.LoadEch0159Schemas();
        var delivery = EchDeserializer.DeserializeXml<Delivery>(xml, schemaSet);
        return FromEventInitialDelivery(delivery);
    }

    private static Contest FromEventInitialDelivery(Delivery delivery)
    {
        var idLookup = new IdLookup();

        var contest = delivery.InitialDelivery.ContestType.ToBasisContest(idLookup);
        contest.Votes = delivery.InitialDelivery.VoteInformation
            .SelectMany(v => v.ToBasisVotes(idLookup))
            .ToList();

        return contest;
    }
}
