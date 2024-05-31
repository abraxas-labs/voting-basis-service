// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Abraxas.Voting.Basis.Shared.V1;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class CountingCircleResultStateDescriptionTest : ProtoValidatorBaseTest<CountingCircleResultStateDescription>
{
    public static CountingCircleResultStateDescription NewValid(Action<CountingCircleResultStateDescription>? action = null)
    {
        var countingCircleResultStateDescription = new CountingCircleResultStateDescription
        {
            State = CountingCircleResultState.AuditedTentatively,
            Description = "geprüft",
        };

        action?.Invoke(countingCircleResultStateDescription);
        return countingCircleResultStateDescription;
    }

    protected override IEnumerable<CountingCircleResultStateDescription> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.Description = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValid(x => x.Description = RandomStringUtil.GenerateSimpleSingleLineText(100));
    }

    protected override IEnumerable<CountingCircleResultStateDescription> NotOkMessages()
    {
        yield return NewValid(x => x.State = CountingCircleResultState.Unspecified);
        yield return NewValid(x => x.State = (CountingCircleResultState)10);
        yield return NewValid(x => x.Description = string.Empty);
        yield return NewValid(x => x.Description = RandomStringUtil.GenerateSimpleSingleLineText(101));
        yield return NewValid(x => x.Description = "vorläufig\n geprüft");
    }
}
