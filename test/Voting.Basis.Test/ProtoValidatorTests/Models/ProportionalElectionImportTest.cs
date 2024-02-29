// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class ProportionalElectionImportTest : ProtoValidatorBaseTest<ProportionalElectionImport>
{
    public static ProportionalElectionImport NewValid(Action<ProportionalElectionImport>? action = null)
    {
        var proportionalElectionImport = new ProportionalElectionImport
        {
            Election = ProportionalElectionTest.NewValid(),
            Lists = { ProportionalElectionListImportTest.NewValid() },
            ListUnions = { ProportionalElectionListUnionTest.NewValid() },
        };

        action?.Invoke(proportionalElectionImport);
        return proportionalElectionImport;
    }

    protected override IEnumerable<ProportionalElectionImport> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.Lists.Clear());
        yield return NewValid(x => x.ListUnions.Clear());
    }

    protected override IEnumerable<ProportionalElectionImport> NotOkMessages()
    {
        yield return NewValid(x => x.Election = null);
    }
}
