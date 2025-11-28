// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Export.Generators.Csv.Converters;

public class SexConverter : DefaultTypeConverter
{
    public override string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
    {
        return value switch
        {
            SexType.Male => "Männlich",
            SexType.Female => "Weiblich",
            _ => string.Empty,
        };
    }
}
