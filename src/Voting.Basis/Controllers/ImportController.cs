// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.IO;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Google.Protobuf;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Voting.Basis.Controllers.Models;
using Voting.Basis.Core.Import;
using Voting.Lib.Iam.Authorization;
using Voting.Lib.Rest.Files;
using Voting.Lib.Rest.Utils;
using ContestImport = Abraxas.Voting.Basis.Services.V1.Models.ContestImport;
using Permissions = Voting.Basis.Core.Auth.Permissions;

namespace Voting.Basis.Controllers;

[ApiController]
[Route("api/imports")]
public class ImportController : ControllerBase
{
    private const long MaxImportRequestSize = 1024L * 1024L * 50; // 50 MB

    private readonly ImportService _importService;
    private readonly IMapper _mapper;
    private readonly MultipartRequestHelper _multipartRequestHelper;

    public ImportController(ImportService importService, IMapper mapper, MultipartRequestHelper multipartRequestHelper)
    {
        _importService = importService;
        _mapper = mapper;
        _multipartRequestHelper = multipartRequestHelper;
    }

    [AuthorizePermission(Permissions.Import.ImportData)]
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImportRequestSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxImportRequestSize)]
    [DisableFormValueModelBinding]
    public async Task<IActionResult> ResolveImportFile(CancellationToken ct)
    {
        Stream? echStream = null;

        try
        {
            var contestImport = await _multipartRequestHelper.ReadFileAndData<ResolveImportFileRequest, ContestImport>(
                Request,
                async data =>
                {
                    var echStream = await BufferToMemoryStream(data.FileContent);
                    var contestImport = _importService.DeserializeImport(data.RequestData.ImportType, echStream, ct);
                    return _mapper.Map<ContestImport>(contestImport);
                },
                null,
                [MediaTypeNames.Text.Xml]);

            return File(contestImport.ToByteArray(), MediaTypeNames.Application.Octet);
        }
        finally
        {
            if (echStream != null)
            {
                await echStream.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private async Task<MemoryStream> BufferToMemoryStream(Stream stream)
    {
        var ms = new MemoryStream();
        await stream.CopyToAsync(ms, HttpContext.RequestAborted);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }
}
