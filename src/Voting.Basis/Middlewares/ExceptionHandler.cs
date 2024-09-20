// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Voting.Basis.Core.Configuration;
using Voting.Basis.Exceptions;
using BaseExceptionHandler = Voting.Lib.Rest.Middleware.ExceptionHandler;

namespace Voting.Basis.Middlewares;

/// <summary>
/// Exception middleware that logs errors and maps them to error responses.
/// </summary>
public class ExceptionHandler : BaseExceptionHandler
{
    public ExceptionHandler(PublisherConfig config, RequestDelegate next, ILogger<ExceptionHandler> logger)
        : base(next, logger, config.EnableDetailedErrors)
    {
    }

    protected override int MapExceptionToStatus(Exception ex)
        => ExceptionMapping.MapToHttpStatusCode(ex);

    protected override bool ExposeExceptionType(Exception ex)
        => ExceptionMapping.ExposeExceptionType(ex);
}
