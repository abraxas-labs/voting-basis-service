// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Lib.Common;

namespace Voting.Basis.Ech.Mapping;

internal static class LanguageMapping
{
    /// <summary>
    /// Transforms a list into a language dictionary.
    /// </summary>
    /// <typeparam name="T">Type of the list.</typeparam>
    /// <param name="src">The source list.</param>
    /// <param name="keySelector">Language key selector.</param>
    /// <param name="valueSelector">Translation value selector.</param>
    /// <returns>The complete translation dictionary.</returns>
    internal static Dictionary<string, string> ToOptionalLanguageDictionary<T>(
        this IEnumerable<T>? src,
        Func<T, string> keySelector,
        Func<T, string?> valueSelector)
    {
        return src
            ?.Select(x => (Language: keySelector(x).ToLower(), Value: valueSelector(x)))
            .Where(x => !string.IsNullOrEmpty(x.Value) && Languages.All.Contains(x.Language))
            .ToDictionary(x => x.Language, x => x.Value!)
            ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Transforms a list into a complete language dictionary. Missing languages will be filled with either an existing non-empty translation value
    /// or the fallback value.
    /// </summary>
    /// <typeparam name="T">Type of the list.</typeparam>
    /// <param name="src">The source list.</param>
    /// <param name="keySelector">Language key selector.</param>
    /// <param name="valueSelector">Translation value selector.</param>
    /// <param name="fallbackValue">The fallback value to use, in case no translation entry exists.</param>
    /// <param name="skipIfNoTranslationExists">Skip filling languages with the fallback value if no translation is available.</param>
    /// <returns>The complete translation dictionary.</returns>
    internal static Dictionary<string, string> ToLanguageDictionary<T>(
        this IEnumerable<T>? src,
        Func<T, string> keySelector,
        Func<T, string> valueSelector,
        string fallbackValue,
        bool skipIfNoTranslationExists = false)
    {
        var translations = src.ToOptionalLanguageDictionary(keySelector, valueSelector);
        return translations.Count == 0 && skipIfNoTranslationExists ? translations : FillAllLanguages(translations, fallbackValue);
    }

    /// <summary>
    /// Ensures that translations in all languages are present. Uses an existing non-empty translation value if one exists, otherwise the fallback value is used.
    /// </summary>
    /// <param name="translations">The translations to fill.</param>
    /// <param name="fallbackValue">The fallback value to use, in case no translation entry exists.</param>
    /// <returns>The filled translations.</returns>
    internal static Dictionary<string, string> FillAllLanguages(Dictionary<string, string> translations, string fallbackValue)
    {
        if (translations.Count == 0)
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
