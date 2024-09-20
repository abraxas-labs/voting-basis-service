// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Common;

namespace Voting.Basis.EventSignature.Models;

public class PublicKeySignatureDeleteAuthenticationTagPayload
{
    public PublicKeySignatureDeleteAuthenticationTagPayload(
        int signatureVersion,
        Guid contestId,
        string hostId,
        string keyId,
        DateTime deletedAt,
        long signedEventCount)
    {
        SignatureVersion = signatureVersion;
        ContestId = contestId;
        HostId = hostId;
        KeyId = keyId;
        DeletedAt = deletedAt;
        SignedEventCount = signedEventCount;
    }

    public int SignatureVersion { get; }

    public Guid ContestId { get; }

    public string HostId { get; }

    public string KeyId { get; }

    public DateTime DeletedAt { get; }

    public long SignedEventCount { get; }

    // changes here are event breaking and need another signature version.
    public byte[] ConvertToBytesToSign()
    {
        using var byteConverter = new ByteConverter();
        return byteConverter
            .Append(SignatureVersion)
            .Append(ContestId.ToString())
            .Append(HostId)
            .Append(KeyId)
            .Append(DeletedAt)
            .Append(SignedEventCount)
            .GetBytes();
    }
}
