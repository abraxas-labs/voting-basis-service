// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Abraxas.Voting.Basis.Shared.V1;
using Google.Protobuf.WellKnownTypes;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.MajorityElection;

public class UpdateMajorityElectionCandidateRequestTest : ProtoValidatorBaseTest<UpdateMajorityElectionCandidateRequest>
{
    protected override IEnumerable<UpdateMajorityElectionCandidateRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Number = RandomStringUtil.GenerateAlphanumericWhitespace(1));
        yield return NewValidRequest(x => x.Number = RandomStringUtil.GenerateAlphanumericWhitespace(10));
        yield return NewValidRequest(x => x.FirstName = RandomStringUtil.GenerateComplexSingleLineText(1));
        yield return NewValidRequest(x => x.FirstName = RandomStringUtil.GenerateComplexSingleLineText(100));
        yield return NewValidRequest(x => x.LastName = RandomStringUtil.GenerateComplexSingleLineText(1));
        yield return NewValidRequest(x => x.LastName = RandomStringUtil.GenerateComplexSingleLineText(100));
        yield return NewValidRequest(x => x.PoliticalFirstName = RandomStringUtil.GenerateComplexSingleLineText(1));
        yield return NewValidRequest(x => x.PoliticalFirstName = RandomStringUtil.GenerateComplexSingleLineText(100));
        yield return NewValidRequest(x => x.PoliticalLastName = RandomStringUtil.GenerateComplexSingleLineText(1));
        yield return NewValidRequest(x => x.PoliticalLastName = RandomStringUtil.GenerateComplexSingleLineText(100));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Occupation, RandomStringUtil.GenerateAlphabetic(2), string.Empty));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Occupation, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Occupation, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(250)));
        yield return NewValidRequest(x => x.Title = string.Empty);
        yield return NewValidRequest(x => x.Title = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValidRequest(x => x.Title = RandomStringUtil.GenerateSimpleSingleLineText(50));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OccupationTitle, RandomStringUtil.GenerateAlphabetic(2), string.Empty));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OccupationTitle, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OccupationTitle, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(250)));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Party, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Party, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(12)));
        yield return NewValidRequest(x => x.Incumbent = false);
        yield return NewValidRequest(x => x.ZipCode = string.Empty);
        yield return NewValidRequest(x => x.ZipCode = RandomStringUtil.GenerateComplexSingleLineText(1));
        yield return NewValidRequest(x => x.ZipCode = RandomStringUtil.GenerateComplexSingleLineText(15));
        yield return NewValidRequest(x => x.Locality = string.Empty);
        yield return NewValidRequest(x => x.Locality = RandomStringUtil.GenerateComplexSingleLineText(1));
        yield return NewValidRequest(x => x.Locality = RandomStringUtil.GenerateComplexSingleLineText(50));
        yield return NewValidRequest(x => x.Position = 1);
        yield return NewValidRequest(x => x.Position = 100);
        yield return NewValidRequest(x => x.Origin = string.Empty);
        yield return NewValidRequest(x => x.Origin = RandomStringUtil.GenerateComplexSingleLineText(1));
        yield return NewValidRequest(x => x.Origin = RandomStringUtil.GenerateComplexSingleLineText(50));
    }

    protected override IEnumerable<UpdateMajorityElectionCandidateRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Id = "invalid-guid");
        yield return NewValidRequest(x => x.Id = string.Empty);
        yield return NewValidRequest(x => x.MajorityElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.MajorityElectionId = string.Empty);
        yield return NewValidRequest(x => x.Number = string.Empty);
        yield return NewValidRequest(x => x.Number = RandomStringUtil.GenerateAlphanumericWhitespace(11));
        yield return NewValidRequest(x => x.Number = "number-2");
        yield return NewValidRequest(x => x.FirstName = string.Empty);
        yield return NewValidRequest(x => x.FirstName = RandomStringUtil.GenerateComplexSingleLineText(101));
        yield return NewValidRequest(x => x.FirstName = "first\nname");
        yield return NewValidRequest(x => x.LastName = string.Empty);
        yield return NewValidRequest(x => x.LastName = RandomStringUtil.GenerateComplexSingleLineText(101));
        yield return NewValidRequest(x => x.LastName = "last\nname");
        yield return NewValidRequest(x => x.PoliticalFirstName = string.Empty);
        yield return NewValidRequest(x => x.PoliticalFirstName = RandomStringUtil.GenerateComplexSingleLineText(101));
        yield return NewValidRequest(x => x.PoliticalFirstName = "pol first\n name");
        yield return NewValidRequest(x => x.PoliticalLastName = string.Empty);
        yield return NewValidRequest(x => x.PoliticalLastName = RandomStringUtil.GenerateComplexSingleLineText(101));
        yield return NewValidRequest(x => x.PoliticalLastName = "pol last\n name");
        yield return NewValidRequest(x => x.DateOfBirth = null);
        yield return NewValidRequest(x => x.Sex = SexType.Unspecified);
        yield return NewValidRequest(x => x.Sex = (SexType)10);
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Occupation, string.Empty, "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Occupation, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Occupation, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Occupation, "de", RandomStringUtil.GenerateComplexSingleLineText(251)));
        yield return NewValidRequest(x => x.Title = RandomStringUtil.GenerateSimpleSingleLineText(51));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OccupationTitle, string.Empty, "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OccupationTitle, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OccupationTitle, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OccupationTitle, "de", RandomStringUtil.GenerateComplexSingleLineText(251)));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Party, string.Empty, "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Party, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Party, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Party, "de", string.Empty));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Party, "de", RandomStringUtil.GenerateComplexSingleLineText(13)));
        yield return NewValidRequest(x => x.ZipCode = RandomStringUtil.GenerateComplexSingleLineText(16));
        yield return NewValidRequest(x => x.ZipCode = "9000\n12");
        yield return NewValidRequest(x => x.Locality = RandomStringUtil.GenerateComplexSingleLineText(51));
        yield return NewValidRequest(x => x.Position = 0);
        yield return NewValidRequest(x => x.Position = 101);
        yield return NewValidRequest(x => x.Origin = RandomStringUtil.GenerateComplexSingleLineText(81));
    }

    private UpdateMajorityElectionCandidateRequest NewValidRequest(Action<UpdateMajorityElectionCandidateRequest>? action = null)
    {
        var request = new UpdateMajorityElectionCandidateRequest
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
            Party = { LanguageUtil.MockAllLanguages("SP") },
            Incumbent = true,
            ZipCode = "1234",
            Locality = "locality",
            Position = 2,
            Origin = "origin",
        };

        action?.Invoke(request);
        return request;
    }
}
