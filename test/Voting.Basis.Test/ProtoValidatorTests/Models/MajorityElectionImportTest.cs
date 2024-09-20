// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class MajorityElectionImportTest : ProtoValidatorBaseTest<MajorityElectionImport>
{
    public static MajorityElectionImport NewValid(Action<MajorityElectionImport>? action = null)
    {
        var majorityElectionImport = new MajorityElectionImport
        {
            Election = MajorityElectionTest.NewValid(),
            Candidates = { MajorityElectionCandidateTest.NewValid() },
        };

        action?.Invoke(majorityElectionImport);
        return majorityElectionImport;
    }

    protected override IEnumerable<MajorityElectionImport> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.Candidates.Clear());
    }

    protected override IEnumerable<MajorityElectionImport> NotOkMessages()
    {
        yield return NewValid(x => x.Election = null);
    }
}
