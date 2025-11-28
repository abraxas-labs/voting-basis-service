// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

namespace Voting.Basis.Core.Export.Generators.Csv;

public class CsvService
{
    private static readonly CsvConfiguration CsvConfiguration = NewCsvConfig();

    public async Task Render<TRow>(PipeWriter writer, IEnumerable<TRow> records, CancellationToken ct = default)
    {
        // use utf8 with bom (excel requires bom)
        await using var streamWriter = new StreamWriter(writer.AsStream(), Encoding.UTF8);
        await using var csvWriter = new CsvWriter(streamWriter, CsvConfiguration);
        await csvWriter.WriteRecordsAsync(records, ct);
    }

    private static CsvConfiguration NewCsvConfig() =>
        new(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
        };
}
