// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using FluentAssertions;
using Voting.Basis.Core.Export;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Unit.Tests.Exports;
public class FileNameUtilTests
{
    [Fact]
    public void GetXmlFileName()
    {
        var result = FileNameUtil.GetXmlFileName(
            "0157",
            "4",
            DomainOfInfluenceCanton.Sg,
            new DateTime(2024, 04, 14),
            "test");

        result.Should().Be("ech0157v4_SG_20240414_test.xml");
    }

    [Fact]
    public void GetZipFileName()
    {
        var result = FileNameUtil.GetZipFileName(
            DomainOfInfluenceCanton.Sg,
            new DateTime(2024, 04, 14),
            "test");

        result.Should().Be("SG_20240414_test.zip");
    }
}
