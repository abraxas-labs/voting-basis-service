// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Abraxas.Voting.Basis.Shared.V1;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.ProportionalElectionUnion;

public class UpdateProportionalElectionUnionPoliticalBusinessesRequestTest : ProtoValidatorBaseTest<UpdateProportionalElectionUnionPoliticalBusinessesRequest>
{
    protected override IEnumerable<UpdateProportionalElectionUnionPoliticalBusinessesRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.ProportionalElectionUnionIds.Clear());
    }

    protected override IEnumerable<UpdateProportionalElectionUnionPoliticalBusinessesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionUnionIds.Add("invalid-guid"));
        yield return NewValidRequest(x => x.ProportionalElectionUnionIds.Add(string.Empty));
        yield return NewValidRequest(x => x.MandateAlgorithm = ProportionalElectionMandateAlgorithm.Unspecified);
        yield return NewValidRequest(x => x.MandateAlgorithm = (ProportionalElectionMandateAlgorithm)10);
    }

    private UpdateProportionalElectionUnionPoliticalBusinessesRequest NewValidRequest(Action<UpdateProportionalElectionUnionPoliticalBusinessesRequest>? action = null)
    {
        var request = new UpdateProportionalElectionUnionPoliticalBusinessesRequest
        {
            ProportionalElectionUnionIds = { "da36912c-7eaf-43fe-86d4-70c816f17c5a" },
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.HagenbachBischoff,
        };

        action?.Invoke(request);
        return request;
    }
}
