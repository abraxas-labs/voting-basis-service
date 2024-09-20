// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class ProportionalElectionListImportTest : ProtoValidatorBaseTest<ProportionalElectionListImport>
{
    public static ProportionalElectionListImport NewValid(Action<ProportionalElectionListImport>? action = null)
    {
        var proportionalElectionListImport = new ProportionalElectionListImport
        {
            List = ProportionalElectionListTest.NewValid(),
            Candidates =
            {
                new ProportionalElectionImportCandidate
                {
                    Candidate = ProportionalElectionCandidateTest.NewValid(),
                },
            },
        };

        action?.Invoke(proportionalElectionListImport);
        return proportionalElectionListImport;
    }

    protected override IEnumerable<ProportionalElectionListImport> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.Candidates.Clear());
    }

    protected override IEnumerable<ProportionalElectionListImport> NotOkMessages()
    {
        yield return NewValid(x => x.List = null);
    }
}
