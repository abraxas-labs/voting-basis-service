// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Voting.Basis.Data.Models;
using Xunit;

namespace Voting.Basis.Test.Extensions;

public class FlattenTest
{
    [Fact]
    public void ShouldWork()
    {
        var data = new List<DomainOfInfluence>
            {
                new DomainOfInfluence
                {
                    Name = "1",
                },
                new DomainOfInfluence
                {
                    Name = "2",
                    Children = new List<DomainOfInfluence>
                    {
                        new DomainOfInfluence
                        {
                            Name = "2.1",
                        },
                    },
                },
            };
        data.Flatten(x => x.Children)
            .Should()
            .HaveCount(3);
    }
}
