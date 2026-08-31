// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Voting.Basis.Data.Models;
using Voting.Lib.Cryptography.X509;

namespace Voting.Basis.Core.Utils;

public static class CertificateChainBuilder
{
    public static TCertificateChain Build<TCertificateChain, TCertificateInfo>(string rawPem, User user, DateTime importedAt)
        where TCertificateChain : BaseCertificateChain<TCertificateInfo>, new()
        where TCertificateInfo : BaseCertificateInfo, new()
    {
        var certificateCollection = new X509Certificate2Collection();
        certificateCollection.ImportFromPem(rawPem);

        var orderedVerifiedCertificates = X509ChainBuilder.VerifyLinearValidCertificateChain(certificateCollection, X509RevocationMode.NoCheck);
        var orderedCertificateInfos = orderedVerifiedCertificates.ConvertAll(c => BuildCertificateInfo<TCertificateInfo>(c));

        for (var i = 0; i < orderedCertificateInfos.Count - 1; i++)
        {
            var certificateInfo = orderedCertificateInfos[i];
            var caCertificateInfo = orderedCertificateInfos[i + 1];
            certificateInfo.CaThumbprint = caCertificateInfo.Thumbprint;
        }

        return new()
        {
            RawPem = rawPem,
            Infos = orderedCertificateInfos,
            ImportedBy = user,
            ImportedAt = importedAt,
        };
    }

    private static TCertificateInfo BuildCertificateInfo<TCertificateInfo>(X509Certificate2 certificate)
        where TCertificateInfo : BaseCertificateInfo, new()
    {
        var sha256Hash = SHA256.HashData(certificate.RawData);

        return new()
        {
            RawPem = certificate.ExportCertificatePem(),
            NotBefore = certificate.NotBefore.ToUniversalTime(),
            NotAfter = certificate.NotAfter.ToUniversalTime(),
            Issuer = certificate.IssuerName.Name,
            Subject = certificate.SubjectName.Name,
            Thumbprint = BitConverter.ToString(sha256Hash).Replace("-", string.Empty).ToUpperInvariant(),
        };
    }
}
