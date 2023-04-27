// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;
using System.Xml.Schema;
using AutoMapper;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Voting.Basis.Core.Exceptions;
using Voting.Lib.Eventing.Exceptions;
using Voting.Lib.Iam.Exceptions;

namespace Voting.Basis.Exceptions;

/// <summary>
/// Maps unhandled exceptions to their respective HTTP or gRPC status code.
/// </summary>
internal readonly struct ExceptionMapping
{
    private const string EnumMappingErrorSource = "AutoMapper.Extensions.EnumMapping";
    private readonly StatusCode _grpcStatusCode;
    private readonly int _httpStatusCode;
    private readonly bool _exposeExceptionType;

    public ExceptionMapping(StatusCode grpcStatusCode, int httpStatusCode, bool exposeExceptionType = false)
    {
        _grpcStatusCode = grpcStatusCode;
        _httpStatusCode = httpStatusCode;
        _exposeExceptionType = exposeExceptionType;
    }

    public static int MapToHttpStatusCode(Exception ex)
        => Map(ex)._httpStatusCode;

    public static StatusCode MapToGrpcStatusCode(Exception ex)
        => Map(ex)._grpcStatusCode;

    public static bool ExposeExceptionType(Exception ex)
        => Map(ex)._exposeExceptionType;

    private static ExceptionMapping Map(Exception ex)
        => ex switch
        {
            NotAuthenticatedException => new(StatusCode.Unauthenticated, StatusCodes.Status401Unauthorized),
            ForbiddenException => new(StatusCode.PermissionDenied, StatusCodes.Status403Forbidden),
            FluentValidation.ValidationException => new(StatusCode.InvalidArgument, StatusCodes.Status400BadRequest),
            EntityNotFoundException => new(StatusCode.NotFound, StatusCodes.Status404NotFound),
            ContestLockedException => new(StatusCode.FailedPrecondition, StatusCodes.Status400BadRequest),
            AggregateNotFoundException => new(StatusCode.NotFound, StatusCodes.Status404NotFound),
            VersionMismatchException => new(StatusCode.Aborted, StatusCodes.Status424FailedDependency),
            AggregateDeletedException => new(StatusCode.NotFound, StatusCodes.Status404NotFound),
            AlreadyExistsException => new(StatusCode.AlreadyExists, StatusCodes.Status424FailedDependency),
            NonUniqueCandidateNumberException => new(StatusCode.AlreadyExists, StatusCodes.Status424FailedDependency, true),
            DuplicatedBfsException => new(StatusCode.AlreadyExists, StatusCodes.Status424FailedDependency, true),
            MajorityElectionWithExistingSecondaryElectionsException => new(StatusCode.FailedPrecondition, StatusCodes.Status424FailedDependency, true),
            ContestTestingPhaseEndedException => new(StatusCode.FailedPrecondition, StatusCodes.Status424FailedDependency),
            ModificationNotAllowedException => new(StatusCode.FailedPrecondition, StatusCodes.Status412PreconditionFailed),
            ContestWithExistingPoliticalBusinessesException => new(StatusCode.FailedPrecondition, StatusCodes.Status412PreconditionFailed, true),
            CountingCircleInScheduledMergeException => new(StatusCode.FailedPrecondition, StatusCodes.Status412PreconditionFailed, true),
            CountingCirclesInScheduledMergeException => new(StatusCode.FailedPrecondition, StatusCodes.Status412PreconditionFailed, true),
            CountingCircleMergerAlreadyActiveException => new(StatusCode.FailedPrecondition, StatusCodes.Status412PreconditionFailed, true),
            ContestSetAsPreviousContestException => new(StatusCode.FailedPrecondition, StatusCodes.Status412PreconditionFailed, true),
            ContestInMergeSetAsPreviousContestException => new(StatusCode.FailedPrecondition, StatusCodes.Status412PreconditionFailed, true),
            AutoMapperMappingException autoMapperException when autoMapperException.InnerException is not null => Map(autoMapperException.InnerException),
            AutoMapperMappingException autoMapperException when string.Equals(autoMapperException.Source, EnumMappingErrorSource) => new(StatusCode.InvalidArgument, StatusCodes.Status400BadRequest),
            ValidationException => new(StatusCode.InvalidArgument, StatusCodes.Status400BadRequest),
            XmlSchemaValidationException => new(StatusCode.InvalidArgument, StatusCodes.Status400BadRequest),
            _ => new(StatusCode.Internal, StatusCodes.Status500InternalServerError),
        };
}
