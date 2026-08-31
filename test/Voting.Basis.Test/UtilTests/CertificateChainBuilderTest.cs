// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using FluentAssertions;
using Snapper;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData.TestFiles;
using Voting.Lib.Cryptography.Exceptions;
using Xunit;

namespace Voting.Basis.Test.UtilTests;

public class CertificateChainBuilderTest
{
    [Fact]
    public void SingleCertInPemShouldWork()
    {
        var certChain = Build(CertificateTestFiles.IsolatedRootRawPem);
        certChain.ShouldMatchSnapshot();
    }

    [Fact]
    public void MultipleCertsInPemShouldWork()
    {
        var certChain = Build(CertificateTestFiles.FourLevelChainRawPem);
        certChain.ShouldMatchSnapshot();
    }

    [Fact]
    public void MultipleCertsInPemMultiPathShouldThrow()
    {
        var ex = Assert.Throws<X509ChainBuilderException>(() => Build(CertificateTestFiles.FourLevelChainWithMultiLeafRawPem));
        ex.Message.Should().Contain("Exactly one leaf certificate is expected, but 2 are provided");
    }

    [Fact]
    public void EmptyPemShouldThrow()
    {
        var ex = Assert.Throws<X509ChainBuilderException>(() => Build(string.Empty));
        ex.Message.Should().Contain("Exactly one leaf certificate is expected, but 0 are provided");
    }

    private CantonSettingsECountingCertificateChain Build(string rawPem) =>
        CertificateChainBuilder.Build<CantonSettingsECountingCertificateChain, CantonSettingsECountingCertificateInfo>(
            rawPem,
            new(),
            new System.DateTime(2020, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc));
}
