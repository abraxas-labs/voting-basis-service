// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Abraxas.Voting.Basis.Shared.V1;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class DomainOfInfluenceVotingCardPrintDataTest : ProtoValidatorBaseTest<DomainOfInfluenceVotingCardPrintData>
{
    public static DomainOfInfluenceVotingCardPrintData NewValid(Action<DomainOfInfluenceVotingCardPrintData>? action = null)
    {
        var domainOfInfluenceVotingCardPrintData = new DomainOfInfluenceVotingCardPrintData
        {
            ShippingAway = VotingCardShippingFranking.A,
            ShippingReturn = VotingCardShippingFranking.A,
            ShippingMethod = VotingCardShippingMethod.OnlyPrintingPackagingToMunicipality,
        };

        action?.Invoke(domainOfInfluenceVotingCardPrintData);
        return domainOfInfluenceVotingCardPrintData;
    }

    protected override IEnumerable<DomainOfInfluenceVotingCardPrintData> OkMessages()
    {
        yield return NewValid();
    }

    protected override IEnumerable<DomainOfInfluenceVotingCardPrintData> NotOkMessages()
    {
        yield return NewValid(x => x.ShippingAway = VotingCardShippingFranking.Unspecified);
        yield return NewValid(x => x.ShippingAway = (VotingCardShippingFranking)10);
        yield return NewValid(x => x.ShippingReturn = VotingCardShippingFranking.Unspecified);
        yield return NewValid(x => x.ShippingReturn = (VotingCardShippingFranking)10);
        yield return NewValid(x => x.ShippingMethod = VotingCardShippingMethod.Unspecified);
        yield return NewValid(x => x.ShippingMethod = (VotingCardShippingMethod)10);
    }
}
