// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Voting.Basis.Controllers.Models;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Export;
using Voting.Basis.Core.Extensions;
using Voting.Lib.Common;
using Voting.Lib.Iam.Authorization;
using Voting.Lib.Rest.Files;

namespace Voting.Basis.Controllers;

[ApiController]
[Route("api/exports")]
public class ExportController : ControllerBase
{
    private readonly ExportService _exportService;
    private readonly IClock _clock;

    public ExportController(ExportService exportService, IClock clock)
    {
        _exportService = exportService;
        _clock = clock;
    }

    [AuthorizePermission(Permissions.Export.ExportData)]
    [HttpPost]
    public async Task<FileResult> GenerateExports([FromBody] GenerateExportRequest request, CancellationToken ct)
    {
        var isMultiExport = _exportService.IsMultipleFileExport(request.Key);
        var exports = _exportService.GenerateExports(request.Key, request.EntityId);
        var files = exports.Select(x => new ExportFileWrapper(x), ct);

        // ZIP file is only created when exporting a contest, otherwise it's a single file.
        if (isMultiExport)
        {
            var fileName = await _exportService.GetZipFileName(request.EntityId);
            return SingleFileResult.CreateZipFile(files, fileName, _clock.UtcNow.ConvertUtcTimeToSwissTime(), ct);
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
