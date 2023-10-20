// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class DomainOfInfluenceVotingCardSwissPostDataTest : ProtoValidatorBaseTest<DomainOfInfluenceVotingCardSwissPostData>
{
    public static DomainOfInfluenceVotingCardSwissPostData NewValid(Action<DomainOfInfluenceVotingCardSwissPostData>? action = null)
    {
        var domainOfInfluenceVotingCardSwissPostData = new DomainOfInfluenceVotingCardSwissPostData
        {
            InvoiceReferenceNumber = "505964478",
            FrankingLicenceReturnNumber = "965333145",
        };

        action?.Invoke(domainOfInfluenceVotingCardSwissPostData);
        return domainOfInfluenceVotingCardSwissPostData;
    }

    protected override IEnumerable<DomainOfInfluenceVotingCardSwissPostData> OkMessages()
    {
        yield return NewValid();
    }

    protected override IEnumerable<DomainOfInfluenceVotingCardSwissPostData> NotOkMessages()
    {
        yield return NewValid(x => x.InvoiceReferenceNumber = string.Empty);
        yield return NewValid(x => x.InvoiceReferenceNumber = RandomStringUtil.GenerateAlphabetic(9));
        yield return NewValid(x => x.InvoiceReferenceNumber = RandomStringUtil.GenerateNumeric(8));
        yield return NewValid(x => x.InvoiceReferenceNumber = RandomStringUtil.GenerateNumeric(10));
        yield return NewValid(x => x.FrankingLicenceReturnNumber = string.Empty);
        yield return NewValid(x => x.FrankingLicenceReturnNumber = RandomStringUtil.GenerateAlphabetic(9));
        yield return NewValid(x => x.FrankingLicenceReturnNumber = RandomStringUtil.GenerateNumeric(8));
        yield return NewValid(x => x.FrankingLicenceReturnNumber = RandomStringUtil.GenerateNumeric(10));
    }
}
