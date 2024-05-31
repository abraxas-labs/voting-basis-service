// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Basis.Core.Auth;

public static class Roles
{
    public const string Admin = "Admin";
    public const string CantonAdmin = "Admin Kanton";
    public const string ElectionAdmin = "Wahlverwalter";
    public const string ElectionSupporter = "Wahlunterstützer";
    public const string ApiReader = "ApiReader";

    public static IEnumerable<string> All()
    {
        yield return Admin;
        yield return CantonAdmin;
        yield return ElectionAdmin;
        yield return ElectionSupporter;
        yield return ApiReader;
    }
}
