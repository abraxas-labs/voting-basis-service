﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Security.Cryptography;
using Voting.Lib.Common;

namespace Voting.Basis.EventSignature.Models;

public class PublicKeySignatureCreateHsmPayload
{
    public PublicKeySignatureCreateHsmPayload(PublicKeySignatureCreateAuthenticationTagPayload authenticationTagPayload, byte[] authenticationTag)
        : this(
            authenticationTagPayload.SignatureVersion,
            authenticationTagPayload.ContestId,
            authenticationTagPayload.HostId,
            authenticationTagPayload.KeyId,
            authenticationTagPayload.PublicKey,
            authenticationTagPayload.ValidFrom,
            authenticationTagPayload.ValidTo,
            authenticationTag)
    {
    }

    public PublicKeySignatureCreateHsmPayload(
        int signatureVersion,
        Guid contestId,
        string hostId,
        string keyId,
        byte[] publicKey,
        DateTime validFrom,
        DateTime validTo,
        byte[] authenticationTag)
    {
        SignatureVersion = signatureVersion;
        ContestId = contestId;
        HostId = hostId;
        KeyId = keyId;
        PublicKey = publicKey;
        ValidFrom = validFrom;
        ValidTo = validTo;
        AuthenticationTag = authenticationTag;
    }

    public int SignatureVersion { get; }

    public Guid ContestId { get; }

    public string HostId { get; }

    public string KeyId { get; }

    public byte[] PublicKey { get; }

    public DateTime ValidFrom { get; }

    public DateTime ValidTo { get; }

    public byte[] AuthenticationTag { get; }

    // changes here are event breaking and need another signature version.
    public byte[] ConvertToBytesToSign()
    {
        using var sha512 = SHA512.Create();
        using var byteConverter = new ByteConverter();
        return byteConverter
            .Append(SignatureVersion)
            .Append(ContestId.ToString())
            .Append(HostId)
            .Append(KeyId)
            .Append(sha512.ComputeHash(PublicKey))
            .Append(ValidFrom)
            .Append(ValidTo)
            .Append(sha512.ComputeHash(AuthenticationTag))
            .GetBytes();
    }
}
