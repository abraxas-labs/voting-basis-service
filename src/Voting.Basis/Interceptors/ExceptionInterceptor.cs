// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Voting.Basis.Core.Configuration;
using Voting.Basis.Exceptions;
using LibExceptionInterceptor = Voting.Lib.Grpc.Interceptors.ExceptionInterceptor;

namespace Voting.Basis.Interceptors;

/// <summary>
/// Logs errors and sets mapped status codes.
/// Currently only implemented for async unary and async server streaming calls since no other call types are used (yet).
/// </summary>
public class ExceptionInterceptor : LibExceptionInterceptor
{
    public ExceptionInterceptor(PublisherConfig config, ILogger<ExceptionInterceptor> logger)
        : base(logger, config.EnableDetailedErrors)
    {
    }

    protected override StatusCode MapExceptionToStatusCode(Exception ex)
        => ExceptionMapping.MapToGrpcStatusCode(ex);

    protected override bool ExposeExceptionType(Exception ex)
        => ExceptionMapping.ExposeExceptionType(ex);
}
