﻿// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Voting.Basis.Core.Export.Models;
using Voting.Lib.VotingExports.Models;

namespace Voting.Basis.Core.Export.Generators;

public interface IExportGenerator
{
    TemplateModel Template { get; }

    Task<ExportFile> GenerateExport(Guid entityId);
}
