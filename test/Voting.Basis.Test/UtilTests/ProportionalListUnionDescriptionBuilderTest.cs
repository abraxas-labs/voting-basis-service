// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using FluentAssertions;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.UtilTests;

public class ProportionalListUnionDescriptionBuilderTest
{
    [Fact]
    public void ShouldWork()
    {
        var descriptions = ProportionalListUnionDescriptionBuilder.BuildListUnionDescription(NewDummyList(), false);

        descriptions[Languages.German]
            .Should()
            .Be("<span><span>01 de</span>, <span class=\"main-list\">02 de</span>, <span>03 de</span></span>");
    }

    [Fact]
    public void ShouldWorkForSubListUnions()
    {
        var descriptions = ProportionalListUnionDescriptionBuilder.BuildListUnionDescription(NewDummyList(), true);

        descriptions[Languages.German]
            .Should()
            .Be("<span><span>04 de</span>, <span>05 de</span>, <span class=\"main-list\">06 de</span></span>");
    }

    [Fact]
    public void ShouldWorkWithoutMainList()
    {
        var l = NewDummyList();
        l.ProportionalElectionListUnionEntries.First()
            .ProportionalElectionListUnion
            .ProportionalElectionMainListId = null;

        var descriptions = ProportionalListUnionDescriptionBuilder.BuildListUnionDescription(l, false);

        descriptions[Languages.German]
            .Should()
            .Be("<span><span>01 de</span>, <span>02 de</span>, <span>03 de</span></span>");
    }

    [Fact]
    public void ShouldTruncateAndPriorizeMainList()
    {
        var l = NewDummyList();
        l.ProportionalElectionListUnionEntries.First()
            .ProportionalElectionListUnion
            .ProportionalElectionListUnionEntries
            .Add(new ProportionalElectionListUnionEntry
            {
                ProportionalElectionList = new ProportionalElectionList
                {
                    ShortDescription = LanguageUtil.MockAllLanguages("99"),
                    Position = 1,
                },
            });
        var descriptions = ProportionalListUnionDescriptionBuilder.BuildListUnionDescription(l, false);

        descriptions[Languages.German]
            .Should()
            .Be("<span><span>01 de</span>, <span>99 de</span>, <span class=\"main-list\">02 de</span>, <span>…</span></span>");
    }

    [Fact]
    public void ShouldEncode()
    {
        var l = NewDummyList();
        l.ProportionalElectionListUnionEntries.First()
            .ProportionalElectionListUnion
            .ProportionalElectionListUnionEntries.First()
            .ProportionalElectionList
            .ShortDescription = LanguageUtil.MockAllLanguages("<script>alert('hi');</script>");

        var descriptions = ProportionalListUnionDescriptionBuilder.BuildListUnionDescription(l, false);

        descriptions[Languages.German]
            .Should()
            .Be("<span><span>&lt;script&gt;alert(&#x27;hi&#x27;);&lt;/script&gt; de</span>, <span class=\"main-list\">02 de</span>, <span>03 de</span></span>");
    }

    [Fact]
    public void ShouldWorkWithMultipleLanguages()
    {
        var l = NewDummyList();
        l.ProportionalElectionListUnionEntries.First()
            .ProportionalElectionListUnion
            .ProportionalElectionListUnionEntries
            .Add(new ProportionalElectionListUnionEntry
            {
                ProportionalElectionList = new ProportionalElectionList
                {
                    ShortDescription =
                    {
                            [Languages.German] = "1g",
                            [Languages.French] = "1f",
                    },
                    Position = 1,
                },
            });
        var descriptions = ProportionalListUnionDescriptionBuilder.BuildListUnionDescription(l, false);

        descriptions[Languages.German]
            .Should()
            .Be("<span><span>01 de</span>, <span>1g</span>, <span class=\"main-list\">02 de</span>, <span>…</span></span>");

        descriptions[Languages.French]
            .Should()
            .Be("<span><span>01 fr</span>, <span>1f</span>, <span class=\"main-list\">02 fr</span>, <span>…</span></span>");
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
