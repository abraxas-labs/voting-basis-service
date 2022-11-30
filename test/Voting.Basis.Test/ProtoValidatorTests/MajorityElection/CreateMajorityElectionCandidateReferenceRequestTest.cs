// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.MajorityElection;

public class CreateMajorityElectionCandidateReferenceRequestTest : ProtoValidatorBaseTest<CreateMajorityElectionCandidateReferenceRequest>
{
    protected override IEnumerable<CreateMajorityElectionCandidateReferenceRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Incumbent = false);
        yield return NewValidRequest(x => x.Position = 1);
        yield return NewValidRequest(x => x.Position = 100);
    }

    protected override IEnumerable<CreateMajorityElectionCandidateReferenceRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.SecondaryMajorityElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.SecondaryMajorityElectionId = string.Empty);
        yield return NewValidRequest(x => x.CandidateId = "invalid-guid");
        yield return NewValidRequest(x => x.CandidateId = string.Empty);
        yield return NewValidRequest(x => x.Position = 0);
        yield return NewValidRequest(x => x.Position = 101);
    }

    private CreateMajorityElectionCandidateReferenceRequest NewValidRequest(Action<CreateMajorityElectionCandidateReferenceRequest>? action = null)
    {
        var request = new CreateMajorityElectionCandidateReferenceRequest
        {
            SecondaryMajorityElectionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            CandidateId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Incumbent = true,
            Position = 27,
        };

        action?.Invoke(request);
        return request;
    }
}
