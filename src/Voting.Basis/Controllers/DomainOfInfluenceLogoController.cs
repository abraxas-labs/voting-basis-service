// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Voting.Basis.Core.Services.Write;

namespace Voting.Basis.Controllers;

[Authorize]
[ApiController]
[Route("api/domain-of-influences/{doiId:guid}/logo")]
public class DomainOfInfluenceLogoController : Controller
{
    private readonly DomainOfInfluenceWriter _writer;

    public DomainOfInfluenceLogoController(DomainOfInfluenceWriter writer)
    {
        _writer = writer;
    }

    [RequestSizeLimit(3_000_000)] // 3MB max size
    [HttpPost]
    public Task SetLogo(Guid doiId, [FromForm] IFormFile logo, CancellationToken ct)
        => _writer.UpdateLogo(
            doiId,
            logo.OpenReadStream(),
            logo.Length,
            logo.ContentType,
            logo.FileName,
            ct);
}
