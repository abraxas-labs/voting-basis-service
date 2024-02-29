// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class PageableTest : ProtoValidatorBaseTest<Pageable>
{
    public static Pageable NewValid(Action<Pageable>? action = null)
    {
        var pageable = new Pageable
        {
            Page = 1234,
            PageSize = 27,
        };

        action?.Invoke(pageable);
        return pageable;
    }

    protected override IEnumerable<Pageable> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.Page = 1);
        yield return NewValid(x => x.Page = 1000000);
        yield return NewValid(x => x.PageSize = 1);
        yield return NewValid(x => x.PageSize = 100);
    }

    protected override IEnumerable<Pageable> NotOkMessages()
    {
        yield return NewValid(x => x.Page = 0);
        yield return NewValid(x => x.Page = 1000001);
        yield return NewValid(x => x.PageSize = 0);
        yield return NewValid(x => x.PageSize = 101);
    }
}
