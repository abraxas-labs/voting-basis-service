// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Ech0010_6_0;
using Ech0044_4_1;
using DataModels = Voting.Basis.Data.Models;

namespace Voting.Basis.Ech.Mapping;

internal static class SexMapping
{
    internal static SexType ToEchSexType(this DataModels.SexType sex)
    {
        return sex switch
        {
            DataModels.SexType.Male => SexType.Item1,
            DataModels.SexType.Female => SexType.Item2,
            _ => SexType.Item3,
        };
    }

    internal static MrMrsType ToEchMrMrsType(this DataModels.SexType sex)
    {
        return sex == DataModels.SexType.Male
            ? MrMrsType.Item2
            : MrMrsType.Item1;
    }

    internal static DataModels.SexType ToBasisSexType(this SexType sex)
    {
        return sex switch
        {
            SexType.Item1 => DataModels.SexType.Male,
            SexType.Item2 => DataModels.SexType.Female,
            _ => throw new ArgumentException($"Sex type {sex} is not valid."),
        };
    }
}
