// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voting.Basis.Controllers.Models;
using Voting.Basis.Core.Export;
using Voting.Basis.Core.Extensions;
using Voting.Lib.Rest.Files;

namespace Voting.Basis.Controllers;

[Authorize]
[ApiController]
[Route("api/exports")]
public class ExportController : ControllerBase
{
    private readonly ExportService _exportService;

    public ExportController(ExportService exportService)
    {
        _exportService = exportService;
    }

    [HttpPost]
    public async Task<FileResult> GenerateExports([FromBody] GenerateExportRequest request, CancellationToken ct)
    {
        var isMultiExport = _exportService.IsMultipleFileExport(request.Key);
        var exports = _exportService.GenerateExports(request.Key, request.EntityId);
        var files = exports.Select(x => new ExportFileWrapper(x), ct);

        // ZIP file is only created when exporting a contest, otherwise it's a single file.
        if (isMultiExport)
        {
            return SingleFileResult.CreateZipFile(files, "contest.zip", ct);
        }

        var enumerator = files.GetAsyncEnumerator(ct);
        if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
        {
            throw new InvalidOperationException("At least one file is required");
        }

        var file = enumerator.Current;
        if (await enumerator.MoveNextAsync().ConfigureAwait(false))
        {
            throw new InvalidOperationException("At maximum one files is supported if " + nameof(isMultiExport) + " is false");
        }

        return SingleFileResult.Create(file, ct);
    }
}
