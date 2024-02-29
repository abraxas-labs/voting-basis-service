// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Utils;
using Voting.Lib.Testing.Validation;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class ProportionalElectionListUnionTest : ProtoValidatorBaseTest<ProtoModels.ProportionalElectionListUnion>
{
    public static ProtoModels.ProportionalElectionListUnion NewValid(Action<ProtoModels.ProportionalElectionListUnion>? action = null)
    {
        var proportionalElectionListUnion = new ProtoModels.ProportionalElectionListUnion
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            ProportionalElectionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Description = { LanguageUtil.MockAllLanguages("Created list") },
            Position = 12,
            ProportionalElectionRootListUnionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            ProportionalElectionListIds = { "da36912c-7eaf-43fe-86d4-70c816f17c5a" },
            ProportionalElectionMainListId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(proportionalElectionListUnion);
        return proportionalElectionListUnion;
    }

    protected override IEnumerable<ProtoModels.ProportionalElectionListUnion> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(255)));
        yield return NewValid(x => x.Position = 1);
        yield return NewValid(x => x.Position = 100);
        yield return NewValid(x => x.ProportionalElectionRootListUnionId = string.Empty);
        yield return NewValid(x => x.ProportionalElectionListIds.Clear());
        yield return NewValid(x => x.ProportionalElectionMainListId = string.Empty);
    }

    protected override IEnumerable<ProtoModels.ProportionalElectionListUnion> NotOkMessages()
    {
        yield return NewValid(x => x.Id = "invalid-guid");
        yield return NewValid(x => x.Id = string.Empty);
        yield return NewValid(x => x.ProportionalElectionId = "invalid-guid");
        yield return NewValid(x => x.ProportionalElectionId = string.Empty);
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Description, string.Empty, "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Description, "de", string.Empty));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Description, "de", RandomStringUtil.GenerateComplexSingleLineText(256)));
        yield return NewValid(x => x.Position = 0);
        yield return NewValid(x => x.Position = 101);
        yield return NewValid(x => x.ProportionalElectionRootListUnionId = "invalid-guid");
        yield return NewValid(x => x.ProportionalElectionListIds.Add("invalid-guid"));
        yield return NewValid(x => x.ProportionalElectionListIds.Add(string.Empty));
        yield return NewValid(x => x.ProportionalElectionMainListId = "invalid-guid");
    }
}
