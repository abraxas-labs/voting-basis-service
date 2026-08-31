// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Data.Models;

public class CantonSettingsECountingCertificateInfo : BaseCertificateInfo
{
    public Guid CertificateChainId { get; set; }

    public CantonSettingsECountingCertificateChain? CertificateChain { get; set; }
}
