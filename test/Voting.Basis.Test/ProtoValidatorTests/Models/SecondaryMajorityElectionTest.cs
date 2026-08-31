// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Utils;
using Voting.Lib.Testing.Validation;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class SecondaryMajorityElectionTest : ProtoValidatorBaseTest<ProtoModels.SecondaryMajorityElection>
{
    public static ProtoModels.SecondaryMajorityElection NewValid(Action<ProtoModels.SecondaryMajorityElection>? action = null)
    {
        var majorityElection = new ProtoModels.SecondaryMajorityElection
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            PrimaryMajorityElectionId = "0a1bf086-4b76-45aa-9f06-02cbfee11f4d",
            PoliticalBusinessNumber = "9468",
            OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Nebenwahl") },
            ShortDescription = { LanguageUtil.MockAllLanguages("Neue Nebenwahl") },
            NumberOfMandates = 5,
            Active = true,
        };

        action?.Invoke(majorityElection);
        return majorityElection;
    }

    protected override IEnumerable<ProtoModels.SecondaryMajorityElection> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.PoliticalBusinessNumber = RandomStringUtil.GenerateAlphanumericWhitespace(1));
        yield return NewValid(x => x.PoliticalBusinessNumber = RandomStringUtil.GenerateAlphanumericWhitespace(10));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexMultiLineText(1)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexMultiLineText(700)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(100)));
        yield return NewValid(x => x.NumberOfMandates = 1);
        yield return NewValid(x => x.NumberOfMandates = 100);
        yield return NewValid(x => x.Active = false);
    }

    protected override IEnumerable<ProtoModels.SecondaryMajorityElection> NotOkMessages()
    {
        yield return NewValid(x => x.Id = "invalid-guid");
        yield return NewValid(x => x.Id = string.Empty);
        yield return NewValid(x => x.PrimaryMajorityElectionId = "invalid-guid");
        yield return NewValid(x => x.PrimaryMajorityElectionId = string.Empty);
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
        yield return NewValid(x => x.NumberOfMandates = 0);
        yield return NewValid(x => x.NumberOfMandates = 101);
    }
}
