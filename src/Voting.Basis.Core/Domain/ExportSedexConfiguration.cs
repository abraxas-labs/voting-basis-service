// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;

namespace Voting.Basis.Core.Domain;

public sealed class ExportSedexConfiguration
{
    public string SenderId { get; set; } = string.Empty;

    public string[] RecipientIds { get; set; } = [];

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

        return Equals((ExportSedexConfiguration)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            SenderId,
            RecipientIds.GetSequenceHashCode());
    }

    private bool Equals(ExportSedexConfiguration other)
    {
        return SenderId == other.SenderId
           && RecipientIds.SequenceEqual(other.RecipientIds);
    }
}
