// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.IO;
using Voting.Lib.Ech;
using Ech0157DeserializerV4 = Voting.Basis.Ech.Converters.V4.Ech0157Deserializer;
using Ech0157DeserializerV5 = Voting.Basis.Ech.Converters.V5.Ech0157Deserializer;
using Ech0159DeserializerV4 = Voting.Basis.Ech.Converters.V4.Ech0159Deserializer;
using Ech0159DeserializerV5 = Voting.Basis.Ech.Converters.V5.Ech0159Deserializer;

namespace Voting.Basis.Ech.Converters;

public class EchDeserializerProvider
{
    private const string Ech0157V4 = "eCH-0157/4";
    private const string Ech0157V5 = "eCH-0157/5";
    private const string Ech0159V4 = "eCH-0159/4";
    private const string Ech0159V5 = "eCH-0159/5";

    private readonly Ech0157DeserializerV4 _ech0157DeserializerV4;
    private readonly Ech0157DeserializerV5 _ech0157DeserializerV5;
    private readonly Ech0159DeserializerV4 _ech0159DeserializerV4;
    private readonly Ech0159DeserializerV5 _ech0159DeserializerV5;

    public EchDeserializerProvider(
        Ech0157DeserializerV4 ech0157DeserializerV4,
        Ech0157DeserializerV5 ech0157DeserializerV5,
        Ech0159DeserializerV4 ech0159DeserializerV4,
        Ech0159DeserializerV5 ech0159DeserializerV5)
    {
        _ech0157DeserializerV4 = ech0157DeserializerV4;
        _ech0157DeserializerV5 = ech0157DeserializerV5;
        _ech0159DeserializerV4 = ech0159DeserializerV4;
        _ech0159DeserializerV5 = ech0159DeserializerV5;
    }

    public IEch0157Deserializer GetEch0157Deserializer(Stream stream)
    {
        return GetSchema(stream, [Ech0157V4, Ech0157V5]) == Ech0157V5 ? _ech0157DeserializerV5 : _ech0157DeserializerV4;
    }

    public IEch0159Deserializer GetEch0159Deserializer(Stream stream)
    {
        return GetSchema(stream, [Ech0159V4, Ech0159V5]) == Ech0159V5 ? _ech0159DeserializerV5 : _ech0159DeserializerV4;
    }

    private static string GetSchema(Stream stream, string[] versions)
    {
        return EchSchemaFinder.GetSchema(stream, versions)
               ?? throw new InvalidOperationException("Cannot determine version");
    }
}
