// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Lib.Common;

namespace Voting.Basis.Core.Export;

internal static class LanguageUtil
{
    // Currently hardcoded, should later be inferred from the preferred user language
    private const string CurrentLanguage = Languages.German;

    /// <summary>
    /// Tries to find the translation in the current language (which is currently hardcoded) and returns it, if it is non-empty.
    /// Otherwise, any other non-emtpy translation is used. If no translations exist, an empty string is returned.
    /// </summary>
    /// <param name="translations">The translation dictionary.</param>
    /// <returns>THe best fitting translation.</returns>
    internal static string GetInCurrentLanguage(Dictionary<string, string> translations)
    {
        if (translations.TryGetValue(CurrentLanguage, out var translation) && !string.IsNullOrEmpty(translation))
        {
            return translation;
        }

        foreach (var lang in Languages.All)
        {
            if (translations.TryGetValue(lang, out var translated) && !string.IsNullOrEmpty(translated))
            {
                return translated;
            }
        }

        return string.Empty;
    }
}
