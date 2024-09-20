// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Basis.Data.Models;
using Voting.Lib.Common;

namespace Voting.Basis.Data.Utils;

/// <summary>
/// Can be used for cases where deterministic id's are required.
/// </summary>
public static class BasisUuidV5
{
    private const string VotingBasisSeparator = ":";

    private static readonly Guid VotingBasisCantonSettingsNamespace = Guid.Parse("d2fb9382-b168-4c8f-9029-af195169962e");
    private static readonly Guid VotingBasisCountingCircleElectorateNamespace = Guid.Parse("049f642c-4355-44e8-8bb2-999efb85bdce");

    // keep in sync with Ausmittlung
    private static readonly Guid VotingBasisProportionalElectionNamespace = Guid.Parse("9602b447-bd9d-4ee0-a15c-94eb2f88e79b");

    public static Guid BuildProportionalElectionEmptyList(Guid proportionalElectionId)
        => Create(VotingBasisProportionalElectionNamespace, proportionalElectionId);

    public static Guid BuildCantonSettings(DomainOfInfluenceCanton canton)
        => UuidV5.Create(VotingBasisCantonSettingsNamespace, GetBigEndianBytes((int)canton));

    public static Guid BuildCountingCircleElectorate(
        Guid countingCircleId,
        IReadOnlyCollection<DomainOfInfluenceType> domainOfInfluenceTypes)
    {
        var domainOfInfluenceTypesId = string.Join(VotingBasisSeparator, domainOfInfluenceTypes);
        return UuidV5.Create(VotingBasisCountingCircleElectorateNamespace, string.Join(VotingBasisSeparator, countingCircleId, domainOfInfluenceTypesId));
    }

    private static Guid Create(Guid ns, params Guid[] existingGuids)
    {
        return UuidV5.Create(
            ns,
            string.Join(VotingBasisSeparator, existingGuids));
    }

    private static byte[] GetBigEndianBytes(int v)
    {
        var bytes = BitConverter.GetBytes(v);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return bytes;
    }
}
