// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using FluentAssertions;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Utils;
using Xunit;

namespace Voting.Basis.Test.UtilTests;

// this test ensures the generated uuid v5 stay consistent across changes
public class BasisUuidV5Test
{
    [Theory]
    [InlineData(DomainOfInfluenceCanton.Sg, "4ce6466f-81f2-5df6-8104-61ff31d89fad")]
    [InlineData(DomainOfInfluenceCanton.Zh, "ac0b262a-7410-5422-9482-c521daa51e7e")]
    [InlineData(DomainOfInfluenceCanton.Tg, "431e7c2b-8151-5eca-a9a3-7010330ba68f")]
    [InlineData(DomainOfInfluenceCanton.Gr, "2ad0ff3b-8beb-507e-9b4c-592dd65887df")]
    [InlineData(DomainOfInfluenceCanton.Ar, "2e9e29e4-c484-5e3e-8c5a-65fd1bc367df")]
    public void BuildCantonSettingsTest(DomainOfInfluenceCanton canton, string expectedGuid)
    {
        BasisUuidV5
            .BuildCantonSettings(canton)
            .Should()
            .Be(Guid.Parse(expectedGuid));
    }
}
