// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.ValueConversion;

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

    /// <summary>
    /// Sets a int list conversion on a enum list property using the <see cref="NpgsqlPropertyBuilderExtensions"/>.
    /// </summary>
    /// <typeparam name="TEnum">The enum.</typeparam>
    /// <param name="builder">The PropertyBuilder.</param>
    /// <returns>The updated property builder.</returns>
    public static PropertyBuilder<List<TEnum>> HasPostgresEnumListToIntListConversion<TEnum>(this PropertyBuilder<List<TEnum>> builder)
        where TEnum : struct
    {
        var elementValueConverter = new ValueConverter<TEnum, int>(
                p => (int)(object)p,
                p => (TEnum)(object)p);

        var converter = new NpgsqlArrayConverter<List<TEnum>, List<int>>(elementValueConverter);

        var comparer = new ValueComparer<List<TEnum>>(
            (l, r) => l != null && r != null && l.SequenceEqual(r),
            v => v.GetSequenceHashCode(),
            v => v.ToList());

        builder.HasPostgresArrayConversion<TEnum, int>(elementValueConverter);
        builder.Metadata.SetValueConverter(converter);
        builder.Metadata.SetValueComparer(comparer);

        return builder;
    }
}
