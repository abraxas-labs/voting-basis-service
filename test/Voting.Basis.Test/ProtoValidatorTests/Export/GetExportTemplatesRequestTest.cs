// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Abraxas.Voting.Basis.Shared.V1;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Export;

public class GetExportTemplatesRequestTest : ProtoValidatorBaseTest<GetExportTemplatesRequest>
{
    protected override IEnumerable<GetExportTemplatesRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.EntityType = ExportEntityType.Unspecified);
    }

    protected override IEnumerable<GetExportTemplatesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.EntityType = (ExportEntityType)(-1));
        yield return NewValidRequest(x => x.Generator = ExportGenerator.Unspecified);
        yield return NewValidRequest(x => x.Generator = (ExportGenerator)(-1));
    }

    private GetExportTemplatesRequest NewValidRequest(Action<GetExportTemplatesRequest>? action = null)
    {
        var request = new GetExportTemplatesRequest
        {
            EntityType = ExportEntityType.Vote,
            Generator = ExportGenerator.VotingBasis,
        };

        action?.Invoke(request);
        return request;
    }
}
