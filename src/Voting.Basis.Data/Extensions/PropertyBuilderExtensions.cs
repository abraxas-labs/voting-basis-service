// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Microsoft.EntityFrameworkCore;

public static class PropertyBuilderExtensions
{
    public static PropertyBuilder<Dictionary<string, string>> HasJsonConversion(this PropertyBuilder<Dictionary<string, string>> propertyBuilder)
    {
        var converter = new ValueConverter<Dictionary<string, string>, string>(
            v => JsonSerializer.Serialize(v, default(JsonSerializerOptions)),
            v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, default(JsonSerializerOptions))!);

        var comparer = new ValueComparer<Dictionary<string, string>>(
            (l, r) => JsonSerializer.Serialize(l, default(JsonSerializerOptions)) == JsonSerializer.Serialize(r, default(JsonSerializerOptions)),
            v => v == null ? 0 : JsonSerializer.Serialize(v, default(JsonSerializerOptions)).GetHashCode(StringComparison.Ordinal),
            v => JsonSerializer.Deserialize<Dictionary<string, string>>(JsonSerializer.Serialize(v, default(JsonSerializerOptions)), default(JsonSerializerOptions))!);

        propertyBuilder.HasConversion(converter);
        propertyBuilder.Metadata.SetValueConverter(converter);
        propertyBuilder.Metadata.SetValueComparer(comparer);
        propertyBuilder.HasColumnType("jsonb");

        return propertyBuilder;
    }
}
