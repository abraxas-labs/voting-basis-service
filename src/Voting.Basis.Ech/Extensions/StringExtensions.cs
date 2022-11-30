// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Ech.Extensions;

public static class StringExtensions
{
    private const string TruncationSuffix = "…";

    public static string Truncate(this string value, int maxLength)
    {
        if (maxLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength));
        }

        return value.Length > maxLength ? value[..(maxLength - TruncationSuffix.Length)] + TruncationSuffix : value;
    }
}
