// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Basis.Test.ProtoValidatorTests.Models;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Import;

public class ImportMajorityElectionCandidatesRequestTest : ProtoValidatorBaseTest<ImportMajorityElectionCandidatesRequest>
{
    protected override IEnumerable<ImportMajorityElectionCandidatesRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Candidates.Clear());
    }

    protected override IEnumerable<ImportMajorityElectionCandidatesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.MajorityElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.MajorityElectionId = string.Empty);
    }

    private ImportMajorityElectionCandidatesRequest NewValidRequest(Action<ImportMajorityElectionCandidatesRequest>? action = null)
    {
        var request = new ImportMajorityElectionCandidatesRequest
        {
            MajorityElectionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Candidates = { MajorityElectionCandidateTest.NewValid() },
        };

        action?.Invoke(request);
        return request;
    }
}
