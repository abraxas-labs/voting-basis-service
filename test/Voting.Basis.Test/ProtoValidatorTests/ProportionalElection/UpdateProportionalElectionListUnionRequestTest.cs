// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.ProportionalElection;

public class UpdateProportionalElectionListUnionRequestTest : ProtoValidatorBaseTest<UpdateProportionalElectionListUnionRequest>
{
    protected override IEnumerable<UpdateProportionalElectionListUnionRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(2), RandomStringUtil.GenerateComplexSingleLineText(255)));
        yield return NewValidRequest(x => x.Position = 1);
        yield return NewValidRequest(x => x.Position = 100);
        yield return NewValidRequest(x => x.ProportionalElectionRootListUnionId = string.Empty);
    }

    protected override IEnumerable<UpdateProportionalElectionListUnionRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Id = "invalid-guid");
        yield return NewValidRequest(x => x.Id = string.Empty);
        yield return NewValidRequest(x => x.ProportionalElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionId = string.Empty);
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, string.Empty, "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(1), "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, RandomStringUtil.GenerateAlphabetic(3), "test"));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, "de", string.Empty));
        yield return NewValidRequest(x => MapFieldUtil.ClearAndAdd(x.Description, "de", RandomStringUtil.GenerateComplexSingleLineText(256)));
        yield return NewValidRequest(x => x.Position = 0);
        yield return NewValidRequest(x => x.Position = 101);
        yield return NewValidRequest(x => x.ProportionalElectionRootListUnionId = "invalid-guid");
    }

    private UpdateProportionalElectionListUnionRequest NewValidRequest(Action<UpdateProportionalElectionListUnionRequest>? action = null)
    {
        var request = new UpdateProportionalElectionListUnionRequest
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            ProportionalElectionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Description = { LanguageUtil.MockAllLanguages("Created list") },
            Position = 12,
            ProportionalElectionRootListUnionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
        };

        action?.Invoke(request);
        return request;
    }
}
