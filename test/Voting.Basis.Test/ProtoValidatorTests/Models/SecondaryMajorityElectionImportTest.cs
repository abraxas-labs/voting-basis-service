// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class SecondaryMajorityElectionImportTest : ProtoValidatorBaseTest<SecondaryMajorityElectionImport>
{
    public static SecondaryMajorityElectionImport NewValid(Action<SecondaryMajorityElectionImport>? action = null)
    {
        var secondaryMajorityElectionImport = new SecondaryMajorityElectionImport
        {
            Election = SecondaryMajorityElectionTest.NewValid(),
            Candidates = { MajorityElectionCandidateTest.NewValid() },
        };

        action?.Invoke(secondaryMajorityElectionImport);
        return secondaryMajorityElectionImport;
    }

    protected override IEnumerable<SecondaryMajorityElectionImport> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.Candidates.Clear());
    }

    protected override IEnumerable<SecondaryMajorityElectionImport> NotOkMessages()
    {
        yield return NewValid(x => x.Election = null);
    }
}
