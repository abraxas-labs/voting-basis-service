// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Ech0155_5_1;
using DataModels = Voting.Basis.Data.Models;

namespace Voting.Basis.Ech.Mapping.V5;

internal static class DomainOfInfluenceMapping
{
    internal static DomainOfInfluenceType ToEchDomainOfInfluence(this DataModels.DomainOfInfluence domainOfInfluence)
    {
        return new()
        {
            DomainOfInfluenceIdentification = domainOfInfluence.Id.ToString(),
            DomainOfInfluenceName = domainOfInfluence.Name,
            DomainOfInfluenceShortname = !string.IsNullOrEmpty(domainOfInfluence.ShortName) ? domainOfInfluence.ShortName.Truncate(5) : null,
            DomainOfInfluenceTypeProperty = ToEchDomainOfInfluenceType(domainOfInfluence.Type),
        };
    }

    private static DomainOfInfluenceTypeType ToEchDomainOfInfluenceType(this DataModels.DomainOfInfluenceType domainOfInfluenceType)
        => Enum.Parse<DomainOfInfluenceTypeType>(domainOfInfluenceType.ToString());
}
