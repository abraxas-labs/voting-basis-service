// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class VoteImportTest : ProtoValidatorBaseTest<VoteImport>
{
    public static VoteImport NewValid(Action<VoteImport>? action = null)
    {
        var voteImport = new VoteImport
        {
            Vote = VoteTest.NewValid(),
        };

        action?.Invoke(voteImport);
        return voteImport;
    }

    protected override IEnumerable<VoteImport> OkMessages()
    {
        yield return NewValid();
    }

    protected override IEnumerable<VoteImport> NotOkMessages()
    {
        yield return NewValid(x => x.Vote = null);
    }
}
