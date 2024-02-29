// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentAssertions;
using Voting.Basis.Core.Utils;
using Xunit;

namespace Voting.Basis.Test.UtilTests;

public class MathUtilsTests
{
    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(2, 2, 1)]
    [InlineData(3, 2, 3)]
    [InlineData(4, 2, 6)]
    [InlineData(4, 3, 4)]
    [InlineData(5, 6, 0)]
    [InlineData(28, 4, 20475)]
    public void BinomialCoefficientTest(int n, int k, int expectedResult)
        => MathUtils.BinomialCoefficient(n, k).Should().Be(expectedResult);
}
