// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Schema;
using Ech0157_4_0;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Mapping.V4;
using Voting.Lib.Common;
using Voting.Lib.Ech;
using Voting.Lib.Ech.Ech0157_4_0.Schemas;

namespace Voting.Basis.Ech.Converters.V4;

public class Ech0157Serializer : IEch0157Serializer
{
    private readonly DeliveryHeaderProvider _deliveryHeaderProvider;
    private readonly EchSerializer _echSerializer;
    private XmlSchemaSet? _echSchemaSet;

    public Ech0157Serializer(DeliveryHeaderProvider deliveryHeaderProvider, EchSerializer echSerializer)
    {
        _deliveryHeaderProvider = deliveryHeaderProvider;
        _echSerializer = echSerializer;
    }

    string IEch0157Serializer.EchVersion => "4";

    /// <summary>
    /// Serialize to eCH-0157.
    /// </summary>
    /// <param name="contest">The contest to serialize.</param>
    /// <param name="majorityElection">The majority election to serialize. It should contain the secondary elections and candidates.</param>
    /// <returns>The serialized eCH-0157 data.</returns>
    public Task<byte[]> ToDelivery(Contest contest, MajorityElection majorityElection)
        => ToDelivery(contest, new[] { majorityElection });

    /// <summary>
    /// Serialize to eCH-0157.
    /// </summary>
    /// <param name="contest">The contest to serialize.</param>
    /// <param name="majorityElections">The majority elections to serialize. They should contain the secondary elections and candidates.</param>
    /// <returns>The serialized eCH-0157 data.</returns>
    public Task<byte[]> ToDelivery(Contest contest, IEnumerable<MajorityElection> majorityElections)
    {
        var contestType = contest.ToEchContestType();
        var electionGroups = majorityElections
            .OrderBy(e => e.PoliticalBusinessNumber)
            .Select(m => m.ToEchElectionGroup(contest.EVoting))
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
    public Task<byte[]> ToDelivery(Contest contest, ProportionalElection proportionalElection)
        => ToDelivery(contest, new[] { proportionalElection });

    /// <summary>
    /// Serialize to eCH-0157.
    /// </summary>
    /// <param name="contest">The contest to serialize.</param>
    /// <param name="proportionalElections">The proportional elections to serialize. They should contain the lists, list unions and candidates.</param>
    /// <returns>The serialized eCH-0157 data.</returns>
    public Task<byte[]> ToDelivery(Contest contest, IEnumerable<ProportionalElection> proportionalElections)
    {
        var contestType = contest.ToEchContestType();
        var electionGroups = proportionalElections
            .OrderBy(e => e.PoliticalBusinessNumber)
            .Select(p => p.ToEchElectionGroup(contest.EVoting))
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

    private async Task<byte[]> ToXmlBytes(Delivery delivery)
    {
        _echSchemaSet ??= Ech0157Schemas.LoadEch0157Schemas();
        await using var memoryStream = new MemoryStream();
        await using var xmlValidationStream = new XmlValidationOnWriteStream(memoryStream, _echSchemaSet);
        _echSerializer.WriteXml(xmlValidationStream, delivery, leaveStreamOpen: true);
        await xmlValidationStream.WaitForValidation();
        return memoryStream.ToArray();
    }
}
