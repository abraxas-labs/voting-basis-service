// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Basis.Core.Export.Models;
using Voting.Lib.VotingExports.Models;

namespace Voting.Basis.Core.Export.Generators;

public interface IExportsGenerator
{
    TemplateModel Template { get; }

    IAsyncEnumerable<ExportFile> GenerateExports(Guid entityId);
}
