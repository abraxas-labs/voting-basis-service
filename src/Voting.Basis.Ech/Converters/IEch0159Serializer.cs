// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Ech.Converters;

public interface IEch0159Serializer
{
    string EchNumber => "0159";

    string EchVersion { get; }

    /// <summary>
    /// Serialize to eCH-0159.
    /// </summary>
    /// <param name="contest">The contest to serialize.</param>
    /// <param name="vote">The vote to serialize. It should contain the ballots, questions and tie break questions.</param>
    /// <returns>The serialized eCH-0159 data.</returns>
    Task<byte[]> ToEventInitialDelivery(Contest contest, Vote vote);

    /// <summary>
    /// Serialize to ECH-159.
    /// </summary>
    /// <param name="contest">The contest to serialize.</param>
    /// <param name="votes">The votes to serialize. They should contain the ballots, questions and tie break questions.</param>
    /// <returns>The serialized eCH-0159 data.</returns>
    Task<byte[]> ToEventInitialDelivery(Contest contest, IEnumerable<Vote> votes);
}
