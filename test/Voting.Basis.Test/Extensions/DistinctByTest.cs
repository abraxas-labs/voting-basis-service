// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Voting.Basis.Test.Extensions;

public class DistinctByTest
{
    [Fact]
    public void ShouldWorkWithSimpleStrings()
    {
        var data = new List<string>
            {
                "a",
                "b",
                "cd",
                "ef",
                "gh",
            };
        data.DistinctBy(x => x.Length)
            .Should()
            .BeEquivalentTo("a", "cd");
    }

    [Fact]
    public void ShouldWorkWithAnonymousKeys()
    {
        var data = new List<string>
            {
                "a",
                "a",
                "b",
                "cd",
                "ce",
                "cf",
                "dd",
            };
        data.DistinctBy(x => new { FirstChar = x[0], x.Length })
            .Should()
            .BeEquivalentTo("a", "b", "cd", "dd");
    }
}
