// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Utils;
using Voting.Lib.Testing.Validation;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class ProportionalElectionListTest : ProtoValidatorBaseTest<ProtoModels.ProportionalElectionList>
{
    public static ProtoModels.ProportionalElectionList NewValid(Action<ProtoModels.ProportionalElectionList>? action = null)
    {
        var proportionalElectionList = new ProtoModels.ProportionalElectionList
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            ProportionalElectionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            OrderNumber = "o1",
            ShortDescription = { LanguageUtil.MockAllLanguages("list") },
            BlankRowCount = 0,
            Position = 1,
            Description = { LanguageUtil.MockAllLanguages("Created list") },
            CountOfCandidates = 27,
            CandidateCountOk = true,
            ListUnionDescription = { LanguageUtil.MockAllLanguages("Created list") },
            SubListUnionDescription = { LanguageUtil.MockAllLanguages("Created list") },
        };

        action?.Invoke(proportionalElectionList);
        return proportionalElectionList;
    }

    protected override IEnumerable<ProtoModels.ProportionalElectionList> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.OrderNumber = RandomStringUtil.GenerateAlphanumericWhitespace(1));
        yield return NewValid(x => x.OrderNumber = RandomStringUtil.GenerateAlphanumericWhitespace(6));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(20)));
        yield return NewValid(x => x.BlankRowCount = 0);
        yield return NewValid(x => x.BlankRowCount = 100);
        yield return NewValid(x => x.Position = 1);
        yield return NewValid(x => x.Position = 100);
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(100)));
        yield return NewValid(x => x.CountOfCandidates = 0);
        yield return NewValid(x => x.CountOfCandidates = 100);
        yield return NewValid(x => x.CandidateCountOk = false);
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ListUnionDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ListUnionDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(255)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.SubListUnionDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.SubListUnionDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(255)));
    }

    protected override IEnumerable<ProtoModels.ProportionalElectionList> NotOkMessages()
    {
        yield return NewValid(x => x.Id = "invalid-guid");
        yield return NewValid(x => x.Id = string.Empty);
        yield return NewValid(x => x.ProportionalElectionId = "invalid-guid");
        yield return NewValid(x => x.ProportionalElectionId = string.Empty);
        yield return NewValid(x => x.OrderNumber = string.Empty);
        yield return NewValid(x => x.OrderNumber = RandomStringUtil.GenerateAlphanumericWhitespace(7));
        yield return NewValid(x => x.OrderNumber = "num-2");
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, string.Empty, "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, "de", string.Empty));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, "de", RandomStringUtil.GenerateComplexSingleLineText(21)));
        yield return NewValid(x => x.BlankRowCount = -1);
        yield return NewValid(x => x.BlankRowCount = 101);
        yield return NewValid(x => x.Position = 0);
        yield return NewValid(x => x.Position = 101);
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Description, string.Empty, "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Description, "de", string.Empty));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Description, "de", RandomStringUtil.GenerateComplexSingleLineText(101)));
        yield return NewValid(x => x.CountOfCandidates = -1);
        yield return NewValid(x => x.CountOfCandidates = 101);
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ListUnionDescription, string.Empty, "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ListUnionDescription, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ListUnionDescription, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ListUnionDescription, "de", string.Empty));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ListUnionDescription, "de", RandomStringUtil.GenerateComplexSingleLineText(256)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.SubListUnionDescription, string.Empty, "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.SubListUnionDescription, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.SubListUnionDescription, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.SubListUnionDescription, "de", string.Empty));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.SubListUnionDescription, "de", RandomStringUtil.GenerateComplexSingleLineText(256)));
    }
}
