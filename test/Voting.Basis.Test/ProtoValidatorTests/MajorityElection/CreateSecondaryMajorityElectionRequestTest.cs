// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.MajorityElection;

public class CreateSecondaryMajorityElectionRequestTest : ProtoValidatorBaseTest<CreateSecondaryMajorityElectionRequest>
{
    protected override IEnumerable<CreateSecondaryMajorityElectionRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.PoliticalBusinessNumber = RandomStringUtil.GenerateAlphanumericWhitespace(1));
        yield return NewValidRequest(x => x.PoliticalBusinessNumber = RandomStringUtil.GenerateAlphanumericWhitespace(10));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexMultiLineText(1)));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexMultiLineText(700)));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(100)));
        yield return NewValidRequest(x => x.NumberOfMandates = 1);
        yield return NewValidRequest(x => x.NumberOfMandates = 100);
        yield return NewValidRequest(x => x.Active = false);
    }

    protected override IEnumerable<CreateSecondaryMajorityElectionRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.PoliticalBusinessNumber = string.Empty);
        yield return NewValidRequest(x => x.PoliticalBusinessNumber = RandomStringUtil.GenerateAlphanumericWhitespace(11));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, string.Empty, "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, "de", string.Empty));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.OfficialDescription, "de", RandomStringUtil.GenerateComplexMultiLineText(701)));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, string.Empty, "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, "de", string.Empty));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.ShortDescription, "de", RandomStringUtil.GenerateComplexSingleLineText(101)));
        yield return NewValidRequest(x => x.NumberOfMandates = 0);
        yield return NewValidRequest(x => x.NumberOfMandates = 101);
        yield return NewValidRequest(x => x.PrimaryMajorityElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.PrimaryMajorityElectionId = string.Empty);
    }

    private CreateSecondaryMajorityElectionRequest NewValidRequest(Action<CreateSecondaryMajorityElectionRequest>? action = null)
    {
        var request = new CreateSecondaryMajorityElectionRequest
        {
            PoliticalBusinessNumber = "10246",
            OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Neben-Majorzwahl") },
            ShortDescription = { LanguageUtil.MockAllLanguages("Neue Neben-Majorzwahl") },
            NumberOfMandates = 5,
            PrimaryMajorityElectionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Active = true,
        };

        action?.Invoke(request);
        return request;
    }
}
