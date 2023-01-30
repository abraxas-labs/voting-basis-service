// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Abraxas.Voting.Basis.Shared.V1;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class PoliticalBusinessTest : ProtoValidatorBaseTest<PoliticalBusiness>
{
    public static PoliticalBusiness NewValid(Action<PoliticalBusiness>? action = null)
    {
        var politicalBusiness = new PoliticalBusiness
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            PoliticalBusinessNumber = "1338",
            OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Abstimmung") },
            ShortDescription = { LanguageUtil.MockAllLanguages("Neue Abst") },
            DomainOfInfluence = DomainOfInfluenceTest.NewValid(),
            PoliticalBusinessType = PoliticalBusinessType.Vote,
            Active = true,
        };

        action?.Invoke(politicalBusiness);
        return politicalBusiness;
    }

    protected override IEnumerable<PoliticalBusiness> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.PoliticalBusinessNumber = RandomStringUtil.GenerateAlphanumericWhitespace(1));
        yield return NewValid(x => x.PoliticalBusinessNumber = RandomStringUtil.GenerateAlphanumericWhitespace(10));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexMultiLineText(1)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexMultiLineText(700)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(100)));
        yield return NewValid(x => x.Active = false);
    }

    protected override IEnumerable<PoliticalBusiness> NotOkMessages()
    {
        yield return NewValid(x => x.Id = "invalid-guid");
        yield return NewValid(x => x.Id = string.Empty);
        yield return NewValid(x => x.PoliticalBusinessNumber = string.Empty);
        yield return NewValid(x => x.PoliticalBusinessNumber = RandomStringUtil.GenerateAlphanumericWhitespace(11));
        yield return NewValid(x => x.PoliticalBusinessNumber = "9468-12");
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, string.Empty, "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, "de", string.Empty));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, "de", RandomStringUtil.GenerateComplexMultiLineText(701)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, string.Empty, "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, "de", string.Empty));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, "de", RandomStringUtil.GenerateComplexSingleLineText(101)));
        yield return NewValid(x => x.DomainOfInfluence = null);
        yield return NewValid(x => x.PoliticalBusinessType = PoliticalBusinessType.Unspecified);
        yield return NewValid(x => x.PoliticalBusinessType = (PoliticalBusinessType)10);
    }
}
