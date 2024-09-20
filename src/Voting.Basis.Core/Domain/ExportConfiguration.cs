// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Domain;

public sealed class ExportConfiguration
{
    public Guid Id { get; set; }

    public string Description { get; set; } = string.Empty;

    public string EaiMessageType { get; set; } = string.Empty;

    public IReadOnlyCollection<string> ExportKeys { get; set; } = Array.Empty<string>();

    public Guid DomainOfInfluenceId { get; set; }

    public ExportProvider Provider { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj == null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((ExportConfiguration)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Id,
            Description,
            EaiMessageType,
            ExportKeys.GetSequenceHashCode(),
            Provider);
    }

    private bool Equals(ExportConfiguration other)
    {
        return Id.Equals(other.Id)
           && Description.Equals(other.Description, StringComparison.Ordinal)
           && EaiMessageType == other.EaiMessageType
           && ExportKeys.SequenceEqual(other.ExportKeys)
           && Provider == other.Provider;
    }
}
