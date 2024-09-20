// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Abraxas.Voting.Basis.Shared.V1;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class CountingCircleElectorateTest : ProtoValidatorBaseTest<CountingCircleElectorate>
{
    public static CountingCircleElectorate NewValid(Action<CountingCircleElectorate>? action = null)
    {
        var electorate = new CountingCircleElectorate
        {
            DomainOfInfluenceTypes =
            {
                DomainOfInfluenceType.Ch,
                DomainOfInfluenceType.An,
            },
        };

        action?.Invoke(electorate);
        return electorate;
    }

    protected override IEnumerable<CountingCircleElectorate> OkMessages()
    {
        yield return NewValid();
    }

    protected override IEnumerable<CountingCircleElectorate> NotOkMessages()
    {
        yield return NewValid(x => x.DomainOfInfluenceTypes.Add(DomainOfInfluenceType.Unspecified));
        yield return NewValid(x => x.DomainOfInfluenceTypes.Add((DomainOfInfluenceType)15));
    }
}
