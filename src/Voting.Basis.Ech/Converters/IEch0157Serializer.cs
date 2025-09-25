// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Ech.Converters;

public interface IEch0157Serializer
{
    string EchNumber => "0157";

    string EchVersion { get; }

    Task<byte[]> ToDelivery(Contest contest, MajorityElection majorityElection);

    /// <summary>
    /// Serialize to eCH-0157.
    /// </summary>
    /// <param name="contest">The contest to serialize.</param>
    /// <param name="majorityElections">The majority elections to serialize. They should contain the secondary elections and candidates.</param>
    /// <returns>The serialized eCH-0157 data.</returns>
    Task<byte[]> ToDelivery(Contest contest, IEnumerable<MajorityElection> majorityElections);

    /// <summary>
    /// Serialize to eCH-0157.
    /// </summary>
    /// <param name="contest">The contest to serialize.</param>
    /// <param name="proportionalElection">The proportional election to serialize. It should contain the lists, list unions and candidates.</param>
    /// <returns>The serialized eCH-0157 data.</returns>
    Task<byte[]> ToDelivery(Contest contest, ProportionalElection proportionalElection);

    /// <summary>
    /// Serialize to eCH-0157.
    /// </summary>
    /// <param name="contest">The contest to serialize.</param>
    /// <param name="proportionalElections">The proportional elections to serialize. They should contain the lists, list unions and candidates.</param>
    /// <returns>The serialized eCH-0157 data.</returns>
    Task<byte[]> ToDelivery(Contest contest, IEnumerable<ProportionalElection> proportionalElections);
}
