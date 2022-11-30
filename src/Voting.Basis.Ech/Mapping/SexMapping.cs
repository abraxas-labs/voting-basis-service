// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using eCH_0010_6_0;
using eCH_0044_4_1;
using DataModels = Voting.Basis.Data.Models;

namespace Voting.Basis.Ech.Mapping;

internal static class SexMapping
{
    internal static SexType ToEchSexType(this DataModels.SexType sex)
    {
        return sex == DataModels.SexType.Male
            ? SexType.Männlich
            : sex == DataModels.SexType.Female
                ? SexType.Weiblich
                : SexType.Unbestimmt;
    }

    internal static MrMrsType ToEchMrMrsType(this DataModels.SexType sex)
    {
        return sex == DataModels.SexType.Male
            ? MrMrsType.Herr
            : MrMrsType.Frau;
    }

    internal static DataModels.SexType ToBasisSexType(this SexType sex)
    {
        return sex == SexType.Männlich
            ? DataModels.SexType.Male
            : sex == SexType.Weiblich
                ? DataModels.SexType.Female
                : DataModels.SexType.Undefined;
    }
}
