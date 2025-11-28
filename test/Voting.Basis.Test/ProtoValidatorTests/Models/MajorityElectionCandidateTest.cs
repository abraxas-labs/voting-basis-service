// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Shared.V1;
using Google.Protobuf.WellKnownTypes;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Utils;
using Voting.Lib.Testing.Validation;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class MajorityElectionCandidateTest : ProtoValidatorBaseTest<ProtoModels.MajorityElectionCandidate>
{
    public static ProtoModels.MajorityElectionCandidate NewValid(Action<ProtoModels.MajorityElectionCandidate>? action = null)
    {
        var majorityElection = new ProtoModels.MajorityElectionCandidate
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            MajorityElectionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Number = "number2",
            FirstName = "firstName",
            LastName = "lastName",
            PoliticalFirstName = "pol first name",
            PoliticalLastName = "pol last name",
            DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            Sex = SexType.Female,
            Occupation = { LanguageUtil.MockAllLanguages("occupation") },
            Title = "title",
            OccupationTitle = { LanguageUtil.MockAllLanguages("occupation title") },
            PartyShortDescription = { LanguageUtil.MockAllLanguages("SP") },
            PartyLongDescription = { LanguageUtil.MockAllLanguages("Sozialdemokratische Partei der Schweiz") },
            Incumbent = true,
            ZipCode = "1234",
            Locality = "locality",
            Position = 2,
            Origin = "origin",
            Street = "street",
            HouseNumber = "1a",
            Country = "CH",
        };

        action?.Invoke(majorityElection);
        return majorityElection;
    }

    protected override IEnumerable<ProtoModels.MajorityElectionCandidate> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.Number = RandomStringUtil.GenerateAlphanumericWhitespace(1));
        yield return NewValid(x => x.Number = RandomStringUtil.GenerateAlphanumericWhitespace(10));
        yield return NewValid(x => x.FirstName = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValid(x => x.FirstName = RandomStringUtil.GenerateSimpleSingleLineText(100));
        yield return NewValid(x => x.LastName = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValid(x => x.LastName = RandomStringUtil.GenerateSimpleSingleLineText(100));
        yield return NewValid(x => x.PoliticalFirstName = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValid(x => x.PoliticalFirstName = RandomStringUtil.GenerateSimpleSingleLineText(100));
        yield return NewValid(x => x.PoliticalLastName = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValid(x => x.PoliticalLastName = RandomStringUtil.GenerateSimpleSingleLineText(100));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Occupation, RandomStringUtil.GenerateAlphabetic(2), string.Empty));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Occupation, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Occupation, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(250)));
        yield return NewValid(x => x.Title = string.Empty);
        yield return NewValid(x => x.Title = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValid(x => x.Title = RandomStringUtil.GenerateSimpleSingleLineText(50));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.OccupationTitle, RandomStringUtil.GenerateAlphabetic(2), string.Empty));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.OccupationTitle, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.OccupationTitle, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(250)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.PartyShortDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.PartyShortDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(12)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.PartyLongDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.PartyLongDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(100)));
        yield return NewValid(x => x.Incumbent = false);
        yield return NewValid(x => x.ZipCode = string.Empty);
        yield return NewValid(x => x.ZipCode = RandomStringUtil.GenerateComplexSingleLineText(1));
        yield return NewValid(x => x.ZipCode = RandomStringUtil.GenerateComplexSingleLineText(15));
        yield return NewValid(x => x.Locality = string.Empty);
        yield return NewValid(x => x.Locality = RandomStringUtil.GenerateComplexSingleLineText(1));
        yield return NewValid(x => x.Locality = RandomStringUtil.GenerateComplexSingleLineText(40));
        yield return NewValid(x => x.Position = 1);
        yield return NewValid(x => x.Position = 100);
        yield return NewValid(x => x.Origin = string.Empty);
        yield return NewValid(x => x.Origin = RandomStringUtil.GenerateComplexSingleLineText(1));
        yield return NewValid(x => x.Origin = RandomStringUtil.GenerateComplexSingleLineText(80));
        yield return NewValid(x => x.DateOfBirth = null);
        yield return NewValid(x => x.Sex = SexType.Unspecified);
    }

    protected override IEnumerable<ProtoModels.MajorityElectionCandidate> NotOkMessages()
    {
        yield return NewValid(x => x.Id = "invalid-guid");
        yield return NewValid(x => x.Id = string.Empty);
        yield return NewValid(x => x.MajorityElectionId = "invalid-guid");
        yield return NewValid(x => x.MajorityElectionId = string.Empty);
        yield return NewValid(x => x.Number = string.Empty);
        yield return NewValid(x => x.Number = RandomStringUtil.GenerateAlphanumericWhitespace(11));
        yield return NewValid(x => x.Number = "number-2");
        yield return NewValid(x => x.FirstName = string.Empty);
        yield return NewValid(x => x.FirstName = RandomStringUtil.GenerateSimpleSingleLineText(101));
        yield return NewValid(x => x.FirstName = "first\nname");
        yield return NewValid(x => x.LastName = string.Empty);
        yield return NewValid(x => x.LastName = RandomStringUtil.GenerateSimpleSingleLineText(101));
        yield return NewValid(x => x.LastName = "last\nname");
        yield return NewValid(x => x.PoliticalFirstName = string.Empty);
        yield return NewValid(x => x.PoliticalFirstName = RandomStringUtil.GenerateSimpleSingleLineText(101));
        yield return NewValid(x => x.PoliticalFirstName = "pol first\n name");
        yield return NewValid(x => x.PoliticalLastName = string.Empty);
        yield return NewValid(x => x.PoliticalLastName = RandomStringUtil.GenerateSimpleSingleLineText(101));
        yield return NewValid(x => x.PoliticalLastName = "pol last\n name");
        yield return NewValid(x => x.Sex = (SexType)10);
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Occupation, string.Empty, "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Occupation, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Occupation, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.Occupation, "de", RandomStringUtil.GenerateComplexSingleLineText(251)));
        yield return NewValid(x => x.Title = RandomStringUtil.GenerateSimpleSingleLineText(51));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.OccupationTitle, string.Empty, "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.OccupationTitle, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.OccupationTitle, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.OccupationTitle, "de", RandomStringUtil.GenerateComplexSingleLineText(251)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.PartyShortDescription, string.Empty, "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.PartyShortDescription, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.PartyShortDescription, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.PartyShortDescription, "de", string.Empty));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.PartyShortDescription, "de", RandomStringUtil.GenerateComplexSingleLineText(13)));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.PartyLongDescription, string.Empty, "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.PartyLongDescription, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.PartyLongDescription, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.PartyLongDescription, "de", string.Empty));
        yield return NewValid(x => MapFieldUtil.ClearAndAdd(x.PartyLongDescription, "de", RandomStringUtil.GenerateComplexSingleLineText(101)));
        yield return NewValid(x => x.ZipCode = RandomStringUtil.GenerateComplexSingleLineText(16));
        yield return NewValid(x => x.ZipCode = "9000\n12");
        yield return NewValid(x => x.Locality = RandomStringUtil.GenerateComplexSingleLineText(41));
        yield return NewValid(x => x.Position = 0);
        yield return NewValid(x => x.Position = 101);
        yield return NewValid(x => x.Origin = RandomStringUtil.GenerateComplexSingleLineText(81));
    }
}
