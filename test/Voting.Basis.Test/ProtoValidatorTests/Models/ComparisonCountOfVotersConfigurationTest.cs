// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Abraxas.Voting.Basis.Shared.V1;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class ComparisonCountOfVotersConfigurationTest : ProtoValidatorBaseTest<ComparisonCountOfVotersConfiguration>
{
    public static ComparisonCountOfVotersConfiguration NewValid(Action<ComparisonCountOfVotersConfiguration>? action = null)
    {
        var comparisonCountOfVotersConfiguration = new ComparisonCountOfVotersConfiguration
        {
            Category = ComparisonCountOfVotersCategory.A,
            ThresholdPercent = 5.5,
        };

        action?.Invoke(comparisonCountOfVotersConfiguration);
        return comparisonCountOfVotersConfiguration;
    }

    protected override IEnumerable<ComparisonCountOfVotersConfiguration> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.ThresholdPercent = 0.0);
        yield return NewValid(x => x.ThresholdPercent = 100.0);
        yield return NewValid(x => x.ThresholdPercent = null);
    }

    protected override IEnumerable<ComparisonCountOfVotersConfiguration> NotOkMessages()
    {
        yield return NewValid(x => x.Category = ComparisonCountOfVotersCategory.Unspecified);
        yield return NewValid(x => x.Category = (ComparisonCountOfVotersCategory)10);
        yield return NewValid(x => x.ThresholdPercent = -1);
        yield return NewValid(x => x.ThresholdPercent = 100.1);
    }
}
