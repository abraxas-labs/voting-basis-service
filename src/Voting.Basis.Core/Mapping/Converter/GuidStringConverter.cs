// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using AutoMapper;
using Voting.Lib.Common;

namespace Voting.Basis.Core.Mapping.Converter;

public class GuidStringConverter :
    ITypeConverter<Guid, string>,
    ITypeConverter<Guid?, string>,
    ITypeConverter<string, Guid?>,
    ITypeConverter<string, Guid>
{
    public string Convert(Guid source, string destination, ResolutionContext context)
        => source.ToString();

    public string Convert(Guid? source, string destination, ResolutionContext context)
        => source == null ? string.Empty : source.Value.ToString();

    public Guid? Convert(string source, Guid? destination, ResolutionContext context)
        => GuidParser.ParseNullable(source);

    public Guid Convert(string source, Guid destination, ResolutionContext context)
        => string.IsNullOrEmpty(source) ? default : GuidParser.Parse(source);
}
