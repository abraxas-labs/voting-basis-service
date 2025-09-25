// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.IO;
using System.Linq;
using Ech0159_4_0;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Mapping;
using Voting.Basis.Ech.Mapping.V4;
using Voting.Lib.Ech;
using Voting.Lib.Ech.Ech0159_4_0.Schemas;

namespace Voting.Basis.Ech.Converters.V4;

public class Ech0159Deserializer
{
    private readonly EchDeserializer _deserializer;

    public Ech0159Deserializer(EchDeserializer deserializer)
    {
        _deserializer = deserializer;
    }

    /// <summary>
    /// Deserialize from eCH-0159.
    /// </summary>
    /// <param name="stream">The input stream.</param>
    /// <returns>The deserialized eCH-0159 contest.</returns>
    public Contest DeserializeXml(Stream stream)
    {
        var schemaSet = Ech0159Schemas.LoadEch0159Schemas();
        var delivery = _deserializer.DeserializeXml<Delivery>(stream, schemaSet);
        return FromEventInitialDelivery(delivery);
    }

    private static Contest FromEventInitialDelivery(Delivery delivery)
    {
        var idLookup = new IdLookup();

        var contest = delivery.InitialDelivery.Contest.ToBasisContest(idLookup);
        contest.Votes = delivery.InitialDelivery.VoteInformation
            .SelectMany(v => v.ToBasisVotes(idLookup))
            .ToList();

        return contest;
    }
}
