// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Basis.Test.ProtoValidatorTests.Models;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.EventLog;

public class ListEventLogsRequestTest : ProtoValidatorBaseTest<ListEventLogsRequest>
{
    protected override IEnumerable<ListEventLogsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ListEventLogsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Pageable = null);
    }

    private ListEventLogsRequest NewValidRequest(Action<ListEventLogsRequest>? action = null)
    {
        var request = new ListEventLogsRequest
        {
            Pageable = PageableTest.NewValid(),
        };

        action?.Invoke(request);
        return request;
    }
}
