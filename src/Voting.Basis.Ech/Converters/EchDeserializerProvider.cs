// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.IO;
using Voting.Lib.Ech;
using Ech0157DeserializerV4 = Voting.Basis.Ech.Converters.V4.Ech0157Deserializer;
using Ech0157DeserializerV51 = Voting.Basis.Ech.Converters.V5_1.Ech0157Deserializer;
using Ech0157DeserializerV52 = Voting.Basis.Ech.Converters.V5_2.Ech0157Deserializer;
using Ech0159DeserializerV4 = Voting.Basis.Ech.Converters.V4.Ech0159Deserializer;
using Ech0159DeserializerV51 = Voting.Basis.Ech.Converters.V5_1.Ech0159Deserializer;
using Ech0159DeserializerV52 = Voting.Basis.Ech.Converters.V5_2.Ech0159Deserializer;

namespace Voting.Basis.Ech.Converters;

public class EchDeserializerProvider
{
    private const string Ech0157V4 = "eCH-0157/4";
    private const string Ech0157V5 = "eCH-0157/5";
    private const string Ech0159V4 = "eCH-0159/4";
    private const string Ech0159V5 = "eCH-0159/5";

    private readonly Dictionary<string, IReadOnlyList<IEchDeserializer>> _eCh0157DeserializersByVersion;
    private readonly Dictionary<string, IReadOnlyList<IEchDeserializer>> _eCh0159DeserializersByVersion;

    public EchDeserializerProvider(
        Ech0157DeserializerV4 ech0157DeserializerV4,
        Ech0157DeserializerV51 ech0157DeserializerV51,
        Ech0157DeserializerV52 ech0157DeserializerV52,
        Ech0159DeserializerV4 ech0159DeserializerV4,
        Ech0159DeserializerV51 ech0159DeserializerV51,
        Ech0159DeserializerV52 ech0159DeserializerV52)
    {
        _eCh0157DeserializersByVersion = new Dictionary<string, IReadOnlyList<IEchDeserializer>>
        {
            [Ech0157V4] = [ech0157DeserializerV4],
            [Ech0157V5] = [ech0157DeserializerV52, ech0157DeserializerV51],
        };
        _eCh0159DeserializersByVersion = new Dictionary<string, IReadOnlyList<IEchDeserializer>>
        {
            [Ech0159V4] = [ech0159DeserializerV4],
            [Ech0159V5] = [ech0159DeserializerV52, ech0159DeserializerV51],
        };
    }

    public IReadOnlyList<IEchDeserializer> GetEch0157Deserializers(Stream stream)
    {
        return GetEchDeserializers(stream, _eCh0157DeserializersByVersion);
    }

    public IReadOnlyList<IEchDeserializer> GetEch0159Deserializers(Stream stream)
    {
        return GetEchDeserializers(stream, _eCh0159DeserializersByVersion);
    }

    private static IReadOnlyList<IEchDeserializer> GetEchDeserializers(Stream stream, Dictionary<string, IReadOnlyList<IEchDeserializer>> serializersByVersion)
    {
        var schemaVersion = GetSchema(stream, serializersByVersion.Keys);
        return serializersByVersion[schemaVersion];
    }

    private static string GetSchema(Stream stream, IReadOnlyCollection<string> versions)
    {
        return EchSchemaFinder.GetSchema(stream, versions)
               ?? throw new InvalidOperationException("Cannot determine version");
    }
}
