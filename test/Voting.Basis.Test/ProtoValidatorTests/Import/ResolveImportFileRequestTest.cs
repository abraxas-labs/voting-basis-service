// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Abraxas.Voting.Basis.Shared.V1;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Import;

public class ResolveImportFileRequestTest : ProtoValidatorBaseTest<ResolveImportFileRequest>
{
    protected override IEnumerable<ResolveImportFileRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ResolveImportFileRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ImportType = ImportType.Unspecified);
        yield return NewValidRequest(x => x.ImportType = (ImportType)10);
    }

    private ResolveImportFileRequest NewValidRequest(Action<ResolveImportFileRequest>? action = null)
    {
        var request = new ResolveImportFileRequest
        {
            FileContent = "test",
            ImportType = ImportType.Ech157,
        };

        action?.Invoke(request);
        return request;
    }
}
