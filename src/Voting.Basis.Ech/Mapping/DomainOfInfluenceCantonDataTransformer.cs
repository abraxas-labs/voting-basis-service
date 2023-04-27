// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Ech.Mapping;

public static class DomainOfInfluenceCantonDataTransformer
{
    internal static string EchCandidateDateOfBirthText(DomainOfInfluenceCanton canton, DateTime dateOfBirth)
    {
        return canton switch
        {
            DomainOfInfluenceCanton.Sg => $"{dateOfBirth:yyyy}",
            _ => $"{dateOfBirth:dd.MM.yyyy}",
        };
    }
}