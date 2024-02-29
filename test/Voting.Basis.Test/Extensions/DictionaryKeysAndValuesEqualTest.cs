// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Voting.Basis.Core.Extensions;
using Xunit;

namespace Voting.Basis.Test.Extensions;

public class DictionaryKeysAndValuesEqualTest
{
    [Fact]
    public void ShouldWorkWithStrings()
    {
        var dict1 = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2",
        };
        var dict2 = new Dictionary<string, string>
        {
            ["key1"] = new StringBuilder().Append("val").Append("ue1").ToString(),
            [new StringBuilder().Append("key").Append('2').ToString()] = "value2",
        };

        dict1.KeysAndValuesEqual(dict2)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void ShouldWorkWithNull()
    {
        var dict1 = new Dictionary<string, string?>
        {
            ["key"] = null,
        };
        var dict2 = new Dictionary<string, string?>
        {
            ["key"] = null,
        };

        dict1.KeysAndValuesEqual(dict2)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void ShouldWorkWithDifferentOrder()
    {
        var dict1 = new Dictionary<int, string>
        {
            [3] = "value3",
            [2] = "value2",
            [1] = "value1",
        };
        var dict2 = new Dictionary<int, string>
        {
            [1] = "value1",
            [2] = "value2",
            [3] = "value3",
        };

        dict1.KeysAndValuesEqual(dict2)
            .Should()
            .BeTrue();
    }
}
