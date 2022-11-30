// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class ContestImportTest : ProtoValidatorBaseTest<ContestImport>
{
    public static ContestImport NewValid(Action<ContestImport>? action = null)
    {
        var contestImport = new ContestImport
        {
            Contest = ContestTest.NewValid(),
            MajorityElections = { MajorityElectionImportTest.NewValid() },
            ProportionalElections = { ProportionalElectionImportTest.NewValid() },
            Votes = { VoteImportTest.NewValid() },
        };

        action?.Invoke(contestImport);
        return contestImport;
    }

    protected override IEnumerable<ContestImport> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.MajorityElections.Clear());
        yield return NewValid(x => x.ProportionalElections.Clear());
        yield return NewValid(x => x.Votes.Clear());
    }

    protected override IEnumerable<ContestImport> NotOkMessages()
    {
        yield return NewValid(x => x.Contest = null);
    }
}
