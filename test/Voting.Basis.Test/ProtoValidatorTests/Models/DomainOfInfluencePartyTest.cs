// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class DomainOfInfluencePartyTest : ProtoValidatorBaseTest<DomainOfInfluenceParty>
{
    public static DomainOfInfluenceParty NewValid(Action<DomainOfInfluenceParty>? action = null)
    {
        var domainOfInfluenceParty = new DomainOfInfluenceParty
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            DomainOfInfluenceId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Name = { LanguageUtil.MockAllLanguages("test") },
            ShortDescription = { LanguageUtil.MockAllLanguages("test") },
        };

        action?.Invoke(domainOfInfluenceParty);
        return domainOfInfluenceParty;
    }

    protected override IEnumerable<DomainOfInfluenceParty> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.Id = string.Empty);
        yield return NewValid(x => x.DomainOfInfluenceId = string.Empty);
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Name, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Name, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(100)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateSimpleSingleLineText(1)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateSimpleSingleLineText(12)));
    }

    protected override IEnumerable<DomainOfInfluenceParty> NotOkMessages()
    {
        yield return NewValid(x => x.Id = "invalid-guid");
        yield return NewValid(x => x.DomainOfInfluenceId = "invalid-guid");
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Name, string.Empty, "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Name, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Name, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Name, RandomStringUtil.GenerateNumeric(2), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Name, "de", string.Empty));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Name, "de", RandomStringUtil.GenerateComplexMultiLineText(101)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Name, "de", "test$"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, string.Empty, "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateNumeric(2), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, "de", string.Empty));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, "de", RandomStringUtil.GenerateSimpleSingleLineText(13)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, "de", "te\nst"));
    }
}
