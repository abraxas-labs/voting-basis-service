// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.IO;
using System.Linq;
using Ech0155_4_0;
using Ech0157_4_0;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Mapping;
using Voting.Lib.Ech;
using Voting.Lib.Ech.Ech0157_4_0.Schemas;

namespace Voting.Basis.Ech.Converters;

public class Ech0157Deserializer
{
    private readonly EchDeserializer _deserializer;

    public Ech0157Deserializer(EchDeserializer deserializer)
    {
        _deserializer = deserializer;
    }

    /// <summary>
    /// Deserialize from eCH-0157.
    /// </summary>
    /// <param name="stream">The input stream.</param>
    /// <returns>The deserialized eCH-0157 contest.</returns>
    public Contest DeserializeXml(Stream stream)
    {
        var schemaSet = Ech0157Schemas.LoadEch0157Schemas();
        var delivery = _deserializer.DeserializeXml<Delivery>(stream, schemaSet);
        return FromEventInitialDelivery(delivery);
    }

    private Contest FromEventInitialDelivery(Delivery delivery)
    {
        var idLookup = new IdLookup();
        var contest = delivery.InitialDelivery.Contest.ToBasisContest(idLookup);

        contest.ProportionalElections = delivery.InitialDelivery.ElectionGroupBallot
            .SelectMany(x => x.ElectionInformation)
            .Where(x => x.Election.TypeOfElection == TypeOfElectionType.Item1)
            .Select(x => x.ToBasisProportionalElection(idLookup))
            .ToList();

        contest.MajorityElections = delivery.InitialDelivery.ElectionGroupBallot
            .SelectMany(x => x.ElectionInformation)
            .Where(x => x.Election.TypeOfElection == TypeOfElectionType.Item2)
            .Select(x => x.ToBasisMajorityElection(idLookup))
            .ToList();

        return contest;
    }
}
