// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Basis.Ech.Mapping;

internal static class UnknownMapping
{
    internal const string UnknownValue = "-";

    internal static string MapUnknownValue(string? value)
    {
        return value is null or UnknownValue ? string.Empty : value;
    }
}
