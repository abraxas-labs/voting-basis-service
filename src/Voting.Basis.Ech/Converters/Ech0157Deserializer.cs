// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using eCH_0155_4_0;
using eCH_0157_4_0;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Mapping;
using Voting.Basis.Ech.Schemas;

namespace Voting.Basis.Ech.Converters;

public static class Ech0157Deserializer
{
    /// <summary>
    /// Deserialize from eCH-0157.
    /// </summary>
    /// <param name="xml">The serialized XML data.</param>
    /// <returns>The deserialized eCH-0157 contest.</returns>
    public static Contest DeserializeXml(string xml)
    {
        var schemaSet = Ech0157SchemaLoader.LoadEch0157Schemas();
        var delivery = EchDeserializer.DeserializeXml<DeliveryType>(xml, schemaSet);
        return FromEventInitialDelivery(delivery);
    }

    private static Contest FromEventInitialDelivery(DeliveryType delivery)
    {
        var idLookup = new IdLookup();
        var contest = delivery.InitialDelivery.Contest.ToBasisContest(idLookup);

        contest.ProportionalElections = delivery.InitialDelivery.ElectionGroupBallot
            .SelectMany(x => x.ElectionInformation)
            .Where(x => x.Election.TypeOfElection == TypeOfElectionType.Proporz)
            .Select(x => x.ToBasisProportionalElection(idLookup))
            .ToList();

        contest.MajorityElections = delivery.InitialDelivery.ElectionGroupBallot
            .SelectMany(x => x.ElectionInformation)
            .Where(x => x.Election.TypeOfElection == TypeOfElectionType.Majorz)
            .Select(x => x.ToBasisMajorityElection(idLookup))
            .ToList();

        return contest;
    }
}
