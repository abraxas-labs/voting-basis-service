// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Basis.Test.ProtoValidatorTests.Models;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Import;

public class ImportContestRequestTest : ProtoValidatorBaseTest<ImportContestRequest>
{
    protected override IEnumerable<ImportContestRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ImportContestRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Contest = null);
    }

    private ImportContestRequest NewValidRequest(Action<ImportContestRequest>? action = null)
    {
        var request = new ImportContestRequest
        {
            Contest = ContestImportTest.NewValid(),
        };

        action?.Invoke(request);
        return request;
    }
}
