// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Domain;

public class CountingCircleElectorate
{
    public Guid Id { get; set; }

    public List<DomainOfInfluenceType> DomainOfInfluenceTypes { get; set; } = new();

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

        return Equals((CountingCircleElectorate)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(DomainOfInfluenceTypes.GetSequenceHashCode());
    }

    private bool Equals(CountingCircleElectorate other)
    {
        return DomainOfInfluenceTypes.SequenceEqual(other.DomainOfInfluenceTypes);
    }
}
