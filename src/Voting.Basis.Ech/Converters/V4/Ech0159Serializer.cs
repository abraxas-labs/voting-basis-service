// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Schema;
using Ech0159_4_0;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Mapping.V4;
using Voting.Lib.Common;
using Voting.Lib.Ech;
using Voting.Lib.Ech.Ech0159_4_0.Schemas;

namespace Voting.Basis.Ech.Converters.V4;

public class Ech0159Serializer : IEch0159Serializer
{
    private readonly DeliveryHeaderProvider _deliveryHeaderProvider;
    private readonly EchSerializer _echSerializer;
    private XmlSchemaSet? _echSchemaSet;

    public Ech0159Serializer(DeliveryHeaderProvider deliveryHeaderProvider, EchSerializer echSerializer)
    {
        _deliveryHeaderProvider = deliveryHeaderProvider;
        _echSerializer = echSerializer;
    }

    string IEch0159Serializer.EchVersion => "4_0";

    /// <summary>
    /// Serialize to eCH-0159.
    /// </summary>
    /// <param name="contest">The contest to serialize.</param>
    /// <param name="vote">The vote to serialize. It should contain the ballots, questions and tie break questions.</param>
    /// <returns>The serialized eCH-0159 data.</returns>
    public Task<byte[]> ToEventInitialDelivery(Contest contest, Vote vote)
        => ToEventInitialDelivery(contest, new[] { vote });

    /// <summary>
    /// Serialize to ECH-159.
    /// </summary>
    /// <param name="contest">The contest to serialize.</param>
    /// <param name="votes">The votes to serialize. They should contain the ballots, questions and tie break questions.</param>
    /// <returns>The serialized eCH-0159 data.</returns>
    public Task<byte[]> ToEventInitialDelivery(Contest contest, IEnumerable<Vote> votes)
    {
        var contestType = contest.ToEchContestType();

        // "VOTING votes" are grouped by their respective domain of influence
        // A vote from this VOTING system represents a ballot inside the eCH vote.
        var voteTypes = votes
            .Where(v => v.Ballots.Count > 0) // The standard requires at least one ballot
            .GroupBy(v => v.DomainOfInfluenceId)
            .Select(x => x.ToEchVoteInformation(contest.EVoting))
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

    private async Task<byte[]> ToXmlBytes(Delivery delivery)
    {
        _echSchemaSet ??= Ech0159Schemas.LoadEch0159Schemas();
        await using var memoryStream = new MemoryStream();
        await using var xmlValidationStream = new XmlValidationOnWriteStream(memoryStream, _echSchemaSet);
        _echSerializer.WriteXml(xmlValidationStream, delivery, leaveStreamOpen: true);
        await xmlValidationStream.WaitForValidation();
        return memoryStream.ToArray();
    }
}
