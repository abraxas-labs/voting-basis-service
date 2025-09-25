// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Basis.Ech.Utils;

internal static class CandidateNumberUtils
{
    internal static string GenerateCandidateReference(string candidateNumber)
        => candidateNumber.PadLeft(2, '0');

    internal static string GenerateCandidateReference(string listOrderNumber, string candidateNumber)
        => $"{listOrderNumber.PadLeft(2, '0')}.{candidateNumber.PadLeft(2, '0')}";
}
