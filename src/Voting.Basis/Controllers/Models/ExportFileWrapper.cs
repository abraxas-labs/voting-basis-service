// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Voting.Basis.Core.Export.Models;
using Voting.Lib.Rest.Files;

namespace Voting.Basis.Controllers.Models;

public class ExportFileWrapper : IFile
{
    private readonly ExportFile _exportFile;

    public ExportFileWrapper(ExportFile exportFile)
    {
        _exportFile = exportFile;
    }

    public string Filename => _exportFile.Filename;

    public string MimeType => _exportFile.ContentType;

    public Task Write(PipeWriter writer, CancellationToken ct = default) => _exportFile.Write(writer, ct);
}
