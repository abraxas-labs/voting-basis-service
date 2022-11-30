// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Basis.Data.Models;
using Voting.Lib.Common;

namespace Voting.Basis.Core.Utils;

/// <summary>
/// Can be used for cases where deterministic id's are required.
/// <remarks>
/// For example for the canton settings:
/// Since only one canton settings can be created
/// a uuid v5 can be used based on the name of the canton.
/// Keep in sync with Voting.Migration!
/// </remarks>
/// </summary>
internal static class BasisUuidV5
{
    private static readonly Guid VotingBasisCantonSettingsNamespace = Guid.Parse("d2fb9382-b168-4c8f-9029-af195169962e");

    internal static Guid BuildCantonSettings(DomainOfInfluenceCanton canton)
        => UuidV5.Create(VotingBasisCantonSettingsNamespace, GetBigEndianBytes((int)canton));

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
