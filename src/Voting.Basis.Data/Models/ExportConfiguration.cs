// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public class ExportConfiguration : BaseEntity
{
    public DomainOfInfluence DomainOfInfluence { get; set; } = null!;

    public Guid DomainOfInfluenceId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string EaiMessageType { get; set; } = string.Empty;

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1819:Properties should not return arrays.",
        Justification = "Simplifies the postgres mapping. Also this value is not really used by Voting.Basis.")]
    public string[] ExportKeys { get; set; } = Array.Empty<string>();

    public ExportProvider Provider { get; set; }
}
