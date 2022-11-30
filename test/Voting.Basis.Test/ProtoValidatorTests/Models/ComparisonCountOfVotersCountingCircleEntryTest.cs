// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Abraxas.Voting.Basis.Shared.V1;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class ComparisonCountOfVotersCountingCircleEntryTest : ProtoValidatorBaseTest<ComparisonCountOfVotersCountingCircleEntry>
{
    public static ComparisonCountOfVotersCountingCircleEntry NewValid(Action<ComparisonCountOfVotersCountingCircleEntry>? action = null)
    {
        var comparisonCountOfVotersCountingCircleEntry = new ComparisonCountOfVotersCountingCircleEntry
        {
            Category = ComparisonCountOfVotersCategory.A,
            CountingCircleId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(comparisonCountOfVotersCountingCircleEntry);
        return comparisonCountOfVotersCountingCircleEntry;
    }

    protected override IEnumerable<ComparisonCountOfVotersCountingCircleEntry> OkMessages()
    {
        yield return NewValid();
    }

    protected override IEnumerable<ComparisonCountOfVotersCountingCircleEntry> NotOkMessages()
    {
        yield return NewValid(x => x.Category = ComparisonCountOfVotersCategory.Unspecified);
        yield return NewValid(x => x.Category = (ComparisonCountOfVotersCategory)10);
        yield return NewValid(x => x.CountingCircleId = "invalid-guid");
        yield return NewValid(x => x.CountingCircleId = string.Empty);
    }
}
