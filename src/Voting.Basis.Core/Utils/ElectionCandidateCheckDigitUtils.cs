// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;

namespace Voting.Basis.Core.Utils;

public static class ElectionCandidateCheckDigitUtils
{
    private const int ModuloDivisor = 11;

    /// <summary>
    /// <para>Calculates a check digit for a candidate number.</para>
    /// <para>
    /// Example:
    /// Candidate number: 02.03
    /// Each digit position is multiplied with a multiplier (each digit position has a own multiplier; for 02.03 -> multipliers 5 4 3 2)
    /// 0 * 5 = 0, 2 * 4 = 8, 0 * 3 = 0, 3 * 2 = 6
    /// The sum is created -> 0 + 8 + 0 + 6 = 14
    /// Modulo 11 is calculated -> 14 % 11 = 3
    /// The remainder is subtracted from 11 and this is the check digit -> 11 - 3 = 8
    /// For candidate number 02.03 the check digit is 8.
    /// </para>
    /// <para>Exceptions: No subtraction is calculated for the remainders 0 and 1. Both result in a check digit of 0.</para>
    /// </summary>
    /// <param name="candidateNumber">The candidate number.</param>
    /// <returns>The check digit.</returns>
    public static int CalculateCheckDigit(string candidateNumber)
    {
        var sum = 0;
        var digits = candidateNumber.Where(char.IsDigit).Select(c => (int)char.GetNumericValue(c)).ToList();
        for (var i = 0; i < digits.Count; i++)
        {
            var multiplier = digits.Count - i + 1;
            sum += digits[i] * multiplier;
        }

        var remainder = sum % ModuloDivisor;

        if (remainder <= 1)
        {
            return 0;
        }

        return ModuloDivisor - remainder;
    }
}
