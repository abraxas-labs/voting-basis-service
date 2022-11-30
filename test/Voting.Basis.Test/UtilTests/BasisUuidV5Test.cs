// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using FluentAssertions;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data.Models;
using Xunit;

namespace Voting.Basis.Test.UtilTests;

// this test ensures the generated uuid v5 stay consistent across changes
public class BasisUuidV5Test
{
    [Theory]
    [InlineData(DomainOfInfluenceCanton.Sg, "4ce6466f-81f2-5df6-8104-61ff31d89fad")]
    [InlineData(DomainOfInfluenceCanton.Zh, "ac0b262a-7410-5422-9482-c521daa51e7e")]
    [InlineData(DomainOfInfluenceCanton.Tg, "431e7c2b-8151-5eca-a9a3-7010330ba68f")]
    public void BuildCantonSettingsTest(DomainOfInfluenceCanton canton, string expectedGuid)
    {
        BasisUuidV5
            .BuildCantonSettings(canton)
            .Should()
            .Be(Guid.Parse(expectedGuid));
    }
}
