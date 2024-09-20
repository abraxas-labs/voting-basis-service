// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;

namespace Voting.Basis.Core.Domain;

/// <summary>
/// A political party belonging to a <see cref="DomainOfInfluence"/>.
/// </summary>
public class DomainOfInfluenceParty
{
    public DomainOfInfluenceParty()
    {
        Name = new Dictionary<string, string>();
        ShortDescription = new Dictionary<string, string>();
    }

    public Guid Id { get; internal set; }

    public Dictionary<string, string> Name { get; private set; }

    public Dictionary<string, string> ShortDescription { get; private set; }

    public Guid DomainOfInfluenceId { get; set; }

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

        return Equals((DomainOfInfluenceParty)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Id,
            Name.GetHashCode(),
            ShortDescription.GetHashCode());
    }

    private bool Equals(DomainOfInfluenceParty other)
    {
        return Id.Equals(other.Id)
            && Name.Count == other.Name.Count
            && !Name.Except(other.Name).Any()
            && ShortDescription.Count == other.ShortDescription.Count
            && !ShortDescription.Except(other.ShortDescription).Any();
    }
}
