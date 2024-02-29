// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Voting.Basis.Core.Export.Models;

public class ExportFile
{
    private readonly byte[] _data;

    public ExportFile(byte[] data, string filename, string contentType)
    {
        _data = data;
        Filename = filename;
        ContentType = contentType;
    }

    public string Filename { get; }

    public string ContentType { get; }

    public async Task Write(PipeWriter writer, CancellationToken ct = default) => await writer.WriteAsync(_data, ct);
}
