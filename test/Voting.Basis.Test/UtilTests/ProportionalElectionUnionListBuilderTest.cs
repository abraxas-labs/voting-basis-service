// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using FluentAssertions;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.UtilTests;

public class ProportionalElectionUnionListBuilderTest
{
    private static readonly Guid UnionId = Guid.Parse("ed76d05c-14fc-4e16-aac5-03e4aae1a409");
    private readonly ProportionalElectionUnionListBuilder _builder = new(null!, null!, null!);

    [Fact]
    public void ShouldBuildUnionLists()
    {
        var unionLists = _builder.BuildUnionLists(UnionId, new()
        {
            NewList(Guid.Parse("2598c164-58db-44bd-865c-08edc7ee26c4"), "01", "SVP", "SVP"),
            NewList(Guid.Parse("be10e99b-bf1c-466b-b9bd-fc60b81e6a3b"), "02", "SP", "SP"),
            NewList(Guid.Parse("92a2ae3f-4863-4364-ae9d-c2e05281b824"), "03", "FDP", "FDP"),
            NewList(Guid.Parse("fa0690cf-c2d5-4691-abb5-ffde7974a70d"), "01", "SVP", "SVP"),
            NewList(Guid.Parse("8e590c39-5993-44c9-ab64-ada7d7439a1f"), "02", "SP", "SP"),
            NewList(Guid.Parse("b73418f3-751c-4d05-93ee-17ad191c0001"), "01", "SVP", "SVP"),
        });
        unionLists.Should().HaveCount(3);
        unionLists.MatchSnapshot();
    }

    [Fact]
    public void ShouldGroupListsByOrderNumberAndGermanTranslation()
    {
        var unionListsWithSameShortDescriptionAndDifferentOrderNumber = _builder.BuildUnionLists(UnionId, new()
        {
            NewList(Guid.Parse("2598c164-58db-44bd-865c-08edc7ee26c4"), "01", "SVP", "SVP"),
            NewList(Guid.Parse("be10e99b-bf1c-466b-b9bd-fc60b81e6a3b"), "02", "SVP", "SVP"),
        });

        unionListsWithSameShortDescriptionAndDifferentOrderNumber.Should().HaveCount(2);

        var unionListsWithSameOrderNumberAndDifferentShortDescription = _builder.BuildUnionLists(UnionId, new()
        {
            NewList(Guid.Parse("2598c164-58db-44bd-865c-08edc7ee26c4"), "01", "SVP", "SVP"),
            NewList(Guid.Parse("be10e99b-bf1c-466b-b9bd-fc60b81e6a3b"), "01", "SP", "SP"),
        });

        unionListsWithSameOrderNumberAndDifferentShortDescription.Should().HaveCount(2);

        var unionListsWithSameOrderNumberAndSameGermanTranslation = _builder.BuildUnionLists(UnionId, new()
        {
            NewList(Guid.Parse("2598c164-58db-44bd-865c-08edc7ee26c4"), "01", "SVP", "SVP", l => l.ShortDescription[Languages.French] = "SVP_FR"),
            NewList(Guid.Parse("be10e99b-bf1c-466b-b9bd-fc60b81e6a3b"), "01", "SVP", "SVP", l => l.ShortDescription[Languages.French] = "SVP-FR"),
        });

        unionListsWithSameOrderNumberAndSameGermanTranslation.Should().HaveCount(1);
    }

    private static ProportionalElectionList NewList(
        Guid id,
        string orderNumber,
        string shortDescription,
        string description,
        Action<ProportionalElectionList>? action = null)
    {
        var list = new ProportionalElectionList
        {
            Id = id,
            OrderNumber = orderNumber,
            ShortDescription = LanguageUtil.MockAllLanguages(shortDescription),
            Description = LanguageUtil.MockAllLanguages(description),
        };

        action?.Invoke(list);
        return list;
    }
}
