// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Abraxas.Voting.Basis.Shared.V1;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Contest;

public class ListContestSummariesRequestTest : ProtoValidatorBaseTest<ListContestSummariesRequest>
{
    protected override IEnumerable<ListContestSummariesRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.States.Clear());
    }

    protected override IEnumerable<ListContestSummariesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.States.Add(ContestState.Unspecified));
        yield return NewValidRequest(x => x.States.Add((ContestState)10));
    }

    private ListContestSummariesRequest NewValidRequest(Action<ListContestSummariesRequest>? action = null)
    {
        var request = new ListContestSummariesRequest
        {
            States = { ContestState.Active },
        };

        action?.Invoke(request);
        return request;
    }
}
