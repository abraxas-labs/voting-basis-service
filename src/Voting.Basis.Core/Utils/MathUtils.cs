// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Basis.Core.Utils;

public static class MathUtils
{
    public static int BinomialCoefficient(int n, int k)
    {
        if (k > n)
        {
            return 0;
        }

        var result = 1;
        for (var i = 1; i <= k; i++)
        {
            result *= n--;
            result /= i;
        }

        return result;
    }
}
