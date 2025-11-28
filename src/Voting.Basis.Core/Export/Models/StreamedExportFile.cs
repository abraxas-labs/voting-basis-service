// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Voting.Basis.Core.Export.Models;

public class StreamedExportFile : ExportFile
{
    private readonly Func<PipeWriter, CancellationToken, Task> _writerFunc;

    public StreamedExportFile(Func<PipeWriter, CancellationToken, Task> writerFunc, string filename, string contentType)
        : base([], filename, contentType)
    {
        _writerFunc = writerFunc;
    }

    public override Task Write(PipeWriter writer, CancellationToken ct = default) => _writerFunc(writer, ct);
}
