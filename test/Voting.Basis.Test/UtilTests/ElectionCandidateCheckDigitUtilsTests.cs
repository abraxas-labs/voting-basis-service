// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using FluentAssertions;
using Voting.Basis.Core.Utils;
using Xunit;

namespace Voting.Basis.Test.UtilTests;

public class ElectionCandidateCheckDigitUtilsTests
{
    [Theory]
    [InlineData("02.03", 8)]
    [InlineData("20.06", 0)]
    [InlineData("05.24", 0)]
    [InlineData("12.12", 2)]
    [InlineData("3", 5)]
    [InlineData("31", 0)]
    [InlineData("126", 0)]
    [InlineData("02a.08", 9)]
    [InlineData("aa", 0)]
    public void CalculateCheckDigitTest(string candidateNumber, int expectedResult)
        => ElectionCandidateCheckDigitUtils.CalculateCheckDigit(candidateNumber).Should().Be(expectedResult);
}
