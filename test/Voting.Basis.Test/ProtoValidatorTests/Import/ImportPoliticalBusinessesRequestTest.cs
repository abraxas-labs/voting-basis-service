// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Basis.Test.ProtoValidatorTests.Models;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Import;

public class ImportPoliticalBusinessesRequestTest : ProtoValidatorBaseTest<ImportPoliticalBusinessesRequest>
{
    protected override IEnumerable<ImportPoliticalBusinessesRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.MajorityElections.Clear());
        yield return NewValidRequest(x => x.ProportionalElections.Clear());
        yield return NewValidRequest(x => x.Votes.Clear());
    }

    protected override IEnumerable<ImportPoliticalBusinessesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ContestId = "invalid-guid");
        yield return NewValidRequest(x => x.ContestId = string.Empty);
    }

    private ImportPoliticalBusinessesRequest NewValidRequest(Action<ImportPoliticalBusinessesRequest>? action = null)
    {
        var request = new ImportPoliticalBusinessesRequest
        {
            ContestId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            MajorityElections = { MajorityElectionImportTest.NewValid() },
            ProportionalElections = { ProportionalElectionImportTest.NewValid() },
            Votes = { VoteImportTest.NewValid() },
        };

        action?.Invoke(request);
        return request;
    }
}
