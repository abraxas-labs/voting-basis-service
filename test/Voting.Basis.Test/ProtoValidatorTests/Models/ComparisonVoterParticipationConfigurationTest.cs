// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Abraxas.Voting.Basis.Shared.V1;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class ComparisonVoterParticipationConfigurationTest : ProtoValidatorBaseTest<ComparisonVoterParticipationConfiguration>
{
    public static ComparisonVoterParticipationConfiguration NewValid(Action<ComparisonVoterParticipationConfiguration>? action = null)
    {
        var comparisonVoterParticipationConfiguration = new ComparisonVoterParticipationConfiguration
        {
            MainLevel = DomainOfInfluenceType.Ch,
            ComparisonLevel = DomainOfInfluenceType.Ct,
            ThresholdPercent = 5.5,
        };

        action?.Invoke(comparisonVoterParticipationConfiguration);
        return comparisonVoterParticipationConfiguration;
    }

    protected override IEnumerable<ComparisonVoterParticipationConfiguration> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.ThresholdPercent = 0.0);
        yield return NewValid(x => x.ThresholdPercent = 100.0);
        yield return NewValid(x => x.ThresholdPercent = null);
    }

    protected override IEnumerable<ComparisonVoterParticipationConfiguration> NotOkMessages()
    {
        yield return NewValid(x => x.MainLevel = DomainOfInfluenceType.Unspecified);
        yield return NewValid(x => x.MainLevel = (DomainOfInfluenceType)(-1));
        yield return NewValid(x => x.ComparisonLevel = DomainOfInfluenceType.Unspecified);
        yield return NewValid(x => x.ComparisonLevel = (DomainOfInfluenceType)(-1));
        yield return NewValid(x => x.ThresholdPercent = -1);
        yield return NewValid(x => x.ThresholdPercent = 100.1);
    }
}
