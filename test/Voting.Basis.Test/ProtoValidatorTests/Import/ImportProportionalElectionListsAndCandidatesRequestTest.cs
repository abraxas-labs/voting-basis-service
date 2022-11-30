// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Basis.Test.ProtoValidatorTests.Models;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Import;

public class ImportProportionalElectionListsAndCandidatesRequestTest : ProtoValidatorBaseTest<ImportProportionalElectionListsAndCandidatesRequest>
{
    protected override IEnumerable<ImportProportionalElectionListsAndCandidatesRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Lists.Clear());
        yield return NewValidRequest(x => x.ListUnions.Clear());
    }

    protected override IEnumerable<ImportProportionalElectionListsAndCandidatesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionId = string.Empty);
    }

    private ImportProportionalElectionListsAndCandidatesRequest NewValidRequest(Action<ImportProportionalElectionListsAndCandidatesRequest>? action = null)
    {
        var request = new ImportProportionalElectionListsAndCandidatesRequest
        {
            ProportionalElectionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Lists = { ProportionalElectionListImportTest.NewValid() },
            ListUnions = { ProportionalElectionListUnionTest.NewValid() },
        };

        action?.Invoke(request);
        return request;
    }
}
