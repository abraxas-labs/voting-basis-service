// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Ech.Mapping;

internal static class DateMapping
{
    internal static DateTime MapToUtcDateTime(this DateTime date)
    {
        // In eCH it is suggested to always use UTC.
        // If we get a date without a timezone, assume it is UTC.
        return date.Kind switch
        {
            DateTimeKind.Utc => date,
            DateTimeKind.Local => date.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(date, DateTimeKind.Utc),
            _ => throw new ArgumentException($"DateTime kind {date.Kind} is not valid."),
        };
    }
}
