// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Lib.Common;

namespace Voting.Basis.Ech.Mapping;

internal static class LanguageMapping
{
    /// <summary>
    /// Transforms a list into a complete language dictionary. Missing languages will be filled with either an existing non-empty translation value
    /// or the fallback value.
    /// </summary>
    /// <typeparam name="T">Type of the list.</typeparam>
    /// <param name="src">The source list.</param>
    /// <param name="keySelector">Language key selector.</param>
    /// <param name="valueSelector">Translation value selector.</param>
    /// <param name="fallbackValue">The fallback value to use, in case no translation entry exists.</param>
    /// <returns>The complete translation dictionary.</returns>
    internal static Dictionary<string, string> ToLanguageDictionary<T>(
        this IEnumerable<T>? src,
        Func<T, string> keySelector,
        Func<T, string> valueSelector,
        string fallbackValue)
    {
        var translations = src?.ToDictionary(keySelector, valueSelector);
        return FillAllLanguages(translations, fallbackValue);
    }

    /// <summary>
    /// Ensures that translations in all languages are present. Uses an existing non-empty translation value if one exists, otherwise the fallback value is used.
    /// </summary>
    /// <param name="translations">The translations to fill.</param>
    /// <param name="fallbackValue">The fallback value to use, in case no translation entry exists.</param>
    /// <returns>The filled translations.</returns>
    private static Dictionary<string, string> FillAllLanguages(Dictionary<string, string>? translations, string fallbackValue)
    {
        if (translations == null || translations.Count == 0)
        {
            return Languages.All.ToDictionary(x => x, _ => fallbackValue);
        }

        var defaultValueToUse = translations.Values.FirstOrDefault(v => !string.IsNullOrEmpty(v))
            ?? fallbackValue;

        foreach (var lang in Languages.All)
        {
            if (!translations.TryGetValue(lang, out var value) || string.IsNullOrEmpty(value))
            {
                translations[lang] = defaultValueToUse;
            }
        }

        return translations;
    }
}
