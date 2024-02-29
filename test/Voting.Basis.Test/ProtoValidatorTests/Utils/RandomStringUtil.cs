// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Voting.Basis.Test.ProtoValidatorTests.Utils;

public static class RandomStringUtil
{
    private static readonly char[] AlphabeticChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();
    private static readonly char[] NumericChars = "0123456789".ToCharArray();
    private static readonly char[] Alphanumeric = AlphabeticChars.Concat(NumericChars).ToArray();
    private static readonly char[] AlphanumericWhitespace = Alphanumeric.Concat(" ").ToArray();
    private static readonly char[] SimpleSingleLineText = Alphanumeric.Concat(" '-".ToCharArray()).ToArray();
    private static readonly char[] SimpleMultiLineText = SimpleSingleLineText.Concat("\r\n".ToCharArray()).ToArray();
    private static readonly char[] ComplexSingleLineText = SimpleSingleLineText.Concat("!?+@,.:()/".ToCharArray()).ToArray();
    private static readonly char[] ComplexMultiLineText = ComplexSingleLineText.Concat("\r\n".ToCharArray()).ToArray();

    public static string GenerateAlphabetic(int size)
    {
        return Generate(size, AlphabeticChars);
    }

    public static string GenerateAlphanumeric(int size)
    {
        return Generate(size, Alphanumeric);
    }

    public static string GenerateAlphanumericWhitespace(int size)
    {
        return Generate(size, AlphanumericWhitespace);
    }

    public static string GenerateNumeric(int size)
    {
        return Generate(size, NumericChars);
    }

    public static string GenerateSimpleSingleLineText(int size)
    {
        return GenerateTrimmedText(size, SimpleSingleLineText);
    }

    public static string GenerateSimpleMultiLineText(int size)
    {
        return GenerateTrimmedText(size, SimpleMultiLineText);
    }

    public static string GenerateComplexSingleLineText(int size)
    {
        return GenerateTrimmedText(size, ComplexSingleLineText);
    }

    public static string GenerateComplexMultiLineText(int size)
    {
        return GenerateTrimmedText(size, ComplexMultiLineText);
    }

    private static string Generate(int size, char[] chars)
    {
        var data = new byte[sizeof(int) * size];
        using (var crypto = RandomNumberGenerator.Create())
        {
            crypto.GetBytes(data);
        }

        var result = new StringBuilder(size);
        for (var i = 0; i < size; i++)
        {
            var rnd = BitConverter.ToUInt32(data, i * sizeof(int));
            var idx = rnd % chars.Length;

            result.Append(chars[idx]);
        }

        return result.ToString();
    }

    private static string GenerateTrimmedText(int size, char[] chars)
    {
        var text = Generate(size, chars).Trim();
        if (text.Length == size)
        {
            return text;
        }

        return text + GenerateAlphabetic(size - text.Length);
    }
}
