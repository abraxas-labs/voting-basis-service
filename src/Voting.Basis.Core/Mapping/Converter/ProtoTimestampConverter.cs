// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;

namespace Voting.Basis.Core.Mapping.Converter;

public class ProtoTimestampConverter :
    ITypeConverter<Timestamp, DateTime>,
    ITypeConverter<Timestamp, DateTime?>,
    ITypeConverter<DateTime, Timestamp>,
    ITypeConverter<DateTime?, Timestamp?>
{
    public DateTime Convert(Timestamp? source, DateTime destination, ResolutionContext context)
        => source?.ToDateTime() ?? default;

    public DateTime? Convert(Timestamp? source, DateTime? destination, ResolutionContext context)
        => source?.ToDateTime();

    // some uninitialized fields may have the value of MinValue with Kind unspecified
    // but ToTimestamp does not work with Kind unspecified, therefore we use an empty Timestamp in this case.
    public Timestamp Convert(DateTime source, Timestamp destination, ResolutionContext context)
        => source == DateTime.MinValue
            ? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc).ToTimestamp()
            : source.ToTimestamp();

    public Timestamp? Convert(DateTime? source, Timestamp? destination, ResolutionContext context)
        => source?.ToTimestamp();
}
