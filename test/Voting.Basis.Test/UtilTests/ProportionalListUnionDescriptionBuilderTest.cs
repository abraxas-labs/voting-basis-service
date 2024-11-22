// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using FluentAssertions;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data.Models;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.UtilTests;

public class ProportionalListUnionDescriptionBuilderTest
{
    [Fact]
    public void ShouldWork()
    {
        var description = ProportionalListUnionDescriptionBuilder.BuildListUnionDescription(NewDummyList(), false);

        description
            .Should()
            .Be("<span><span>01</span>, <span class=\"main-list\">02</span>, <span>03</span></span>");
    }

    [Fact]
    public void ShouldWorkForSubListUnions()
    {
        var description = ProportionalListUnionDescriptionBuilder.BuildListUnionDescription(NewDummyList(), true);

        description
            .Should()
            .Be("<span><span>04</span>, <span>05</span>, <span class=\"main-list\">06</span></span>");
    }

    [Fact]
    public void ShouldWorkWithoutMainList()
    {
        var l = NewDummyList();
        l.ProportionalElectionListUnionEntries.First()
            .ProportionalElectionListUnion
            .ProportionalElectionMainListId = null;

        var description = ProportionalListUnionDescriptionBuilder.BuildListUnionDescription(l, false);

        description
            .Should()
            .Be("<span><span>01</span>, <span>02</span>, <span>03</span></span>");
    }

    [Fact]
    public void ShouldTruncateAndPriorizeMainList()
    {
        var l = NewDummyList();
        var listUnionEntries = l.ProportionalElectionListUnionEntries.First()
            .ProportionalElectionListUnion
            .ProportionalElectionListUnionEntries;
        for (var i = 0; i < 5; i++)
        {
            listUnionEntries.Add(new ProportionalElectionListUnionEntry
            {
                ProportionalElectionList = new ProportionalElectionList
                {
                    Id = Guid.NewGuid(),
                    ShortDescription = LanguageUtil.MockAllLanguages((99 - i).ToString()),
                    OrderNumber = (99 - i).ToString(),
                    Position = 1,
                },
            });
        }

        var description = ProportionalListUnionDescriptionBuilder.BuildListUnionDescription(l, false);

        description
            .Should()
            .Be("<span><span>01</span>, <span>99</span>, <span>98</span>, <span>97</span>, <span>96</span>, <span>95</span>, <span class=\"main-list\">02</span>, <span>…</span></span>");
    }

    [Fact]
    public void ShouldEncode()
    {
        var l = NewDummyList();
        l.ProportionalElectionListUnionEntries.First()
            .ProportionalElectionListUnion
            .ProportionalElectionListUnionEntries.First()
            .ProportionalElectionList
            .OrderNumber = "<script>alert('hi');</script>";

        var description = ProportionalListUnionDescriptionBuilder.BuildListUnionDescription(l, false);

        description
            .Should()
            .Be("<span><span>&lt;script&gt;alert(&#x27;hi&#x27;);&lt;/script&gt;</span>, <span class=\"main-list\">02</span>, <span>03</span></span>");
    }

    private static ProportionalElectionList NewDummyList()
    {
        var listUnionId1 = Guid.Parse("d18b47c3-de4d-46d5-9b23-341f214b6b2b");
        var list1Id = Guid.Parse("ac58f2de-4425-4569-8eb1-45f15242ad41");
        var list2Id = Guid.Parse("db2bdbe3-52a0-49f2-b77e-a6e61e6cc963");
        var list3Id = Guid.Parse("a89911e1-f0cb-42a3-b972-e5d2fa1faa7a");
        var list4Id = Guid.Parse("24888187-43a6-45c4-a36d-7a007ba25435");
        var list5Id = Guid.Parse("187332f0-adee-4d3a-ac16-6cc7d0242960");
        var list6Id = Guid.Parse("5f2ab3cb-f288-4ec8-a7fb-87eef06213ef");
        return new ProportionalElectionList
        {
            ProportionalElectionListUnionEntries =
                {
                    new ProportionalElectionListUnionEntry
                    {
                        ProportionalElectionListUnion = new ProportionalElectionListUnion
                        {
                            Id = listUnionId1,
                            ProportionalElectionMainListId = list2Id,
                            ProportionalElectionListUnionEntries =
                            {
                                new ProportionalElectionListUnionEntry
                                {
                                    ProportionalElectionListId = list1Id,
                                    ProportionalElectionList = new ProportionalElectionList
                                    {
                                        ShortDescription = LanguageUtil.MockAllLanguages("01"),
                                        OrderNumber = "01",
                                        Id = list1Id,
                                        Position = 1,
                                    },
                                },
                                new ProportionalElectionListUnionEntry
                                {
                                    ProportionalElectionListId = list2Id,
                                    ProportionalElectionList = new ProportionalElectionList
                                    {
                                        ShortDescription = LanguageUtil.MockAllLanguages("02"),
                                        OrderNumber = "02",
                                        Id = list2Id,
                                        Position = 2,
                                    },
                                },
                                new ProportionalElectionListUnionEntry
                                {
                                    ProportionalElectionListId = list3Id,
                                    ProportionalElectionList = new ProportionalElectionList
                                    {
                                        ShortDescription = LanguageUtil.MockAllLanguages("03"),
                                        OrderNumber = "03",
                                        Id = list3Id,
                                        Position = 3,
                                    },
                                },
                            },
                        },
                    },
                    new ProportionalElectionListUnionEntry
                    {
                        ProportionalElectionListUnion = new ProportionalElectionListUnion
                        {
                            ProportionalElectionRootListUnionId = listUnionId1,
                            ProportionalElectionMainListId = list6Id,
                            ProportionalElectionListUnionEntries =
                            {
                                new ProportionalElectionListUnionEntry
                                {
                                    ProportionalElectionListId = list4Id,
                                    ProportionalElectionList = new ProportionalElectionList
                                    {
                                        ShortDescription = LanguageUtil.MockAllLanguages("04"),
                                        OrderNumber = "04",
                                        Id = list4Id,
                                        Position = 4,
                                    },
                                },
                                new ProportionalElectionListUnionEntry
                                {
                                    ProportionalElectionListId = list5Id,
                                    ProportionalElectionList = new ProportionalElectionList
                                    {
                                        ShortDescription = LanguageUtil.MockAllLanguages("05"),
                                        OrderNumber = "05",
                                        Id = list5Id,
                                        Position = 5,
                                    },
                                },
                                new ProportionalElectionListUnionEntry
                                {
                                    ProportionalElectionListId = list6Id,
                                    ProportionalElectionList = new ProportionalElectionList
                                    {
                                        ShortDescription = LanguageUtil.MockAllLanguages("06"),
                                        OrderNumber = "06",
                                        Id = list6Id,
                                        Position = 6,
                                    },
                                },
                            },
                        },
                    },
                },
        };
    }
}
