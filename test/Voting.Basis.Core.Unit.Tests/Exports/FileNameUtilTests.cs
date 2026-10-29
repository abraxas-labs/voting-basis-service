// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using FluentAssertions;
using Voting.Basis.Core.Export;
using Voting.Basis.Data.Models;
using Voting.Lib.VotingExports.Repository.Basis;

namespace Voting.Basis.Core.Unit.Tests.Exports;
public class FileNameUtilTests
{
    [Fact]
    public void GetXmlFileName()
    {
        var result = FileNameUtil.GetXmlFileName(
            BasisXmlMajorityElectionTemplates.Ech0157_4_0.Filename,
            DomainOfInfluenceCanton.Sg,
            new DateTime(2024, 04, 14),
            "test");

        result.Should().Be("eCH-0157_v4_0_SG_20240414_test.xml");
    }

    [Fact]
    public void GetXmlEVotingFileName()
    {
        var result = FileNameUtil.GetXmlFileName(
            BasisXmlContestTemplates.Ech0157And0159_5_2_EVotingOnly.Filename,
            DomainOfInfluenceCanton.Sg,
            new DateTime(2024, 04, 14),
            "majority_elections",
            "-0157");

        result.Should().Be("eCH-0157_v4_3_SG_20240414_majority_elections_e_voting.xml");
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
