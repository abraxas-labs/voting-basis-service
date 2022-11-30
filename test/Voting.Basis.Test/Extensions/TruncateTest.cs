// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using FluentAssertions;
using Voting.Basis.Ech.Extensions;
using Xunit;

namespace Voting.Basis.Test.Extensions;

public class TruncateTest
{
    [Fact]
    public void ShouldWork()
    {
        const string data = "Test-String";
        data.Truncate(5).Should().Be("Test…");
        data.Truncate(15).Should().Be("Test-String");
        data.Truncate(1).Should().Be("…");
    }

    [Fact]
    public void ShouldThrow()
    {
        const string data = "Test-String";
        Assert.Throws<ArgumentOutOfRangeException>(() => data.Truncate(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => data.Truncate(-5));
    }
}
