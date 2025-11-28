// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Voting.Basis.Core.Export.Generators.Csv.Converters;

public class BooleanConverter : DefaultTypeConverter
{
    public override string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        => value switch
        {
            true => "Ja",
            false => "Nein",
            _ => string.Empty,
        };
}
