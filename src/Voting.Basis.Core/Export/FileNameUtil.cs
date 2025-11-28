// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Export;
public static class FileNameUtil
{
    public static string GetXmlFileName(string echNumber, string echVersion, DomainOfInfluenceCanton canton, DateTime date, string description)
    {
        var cantonFormatted = FormatCanton(canton);
        var dateFormatted = FormatDate(date);

        return $"eCH-{echNumber}_v{echVersion}_{cantonFormatted}_{dateFormatted}_{description}{FileExtensions.Xml}";
    }

    public static string GetZipFileName(DomainOfInfluenceCanton canton, DateTime date, string description)
    {
        var cantonFormatted = FormatCanton(canton);
        var dateFormatted = FormatDate(date);

        return $"{cantonFormatted}_{dateFormatted}_{description}{FileExtensions.Zip}";
    }

    private static string FormatCanton(DomainOfInfluenceCanton canton)
    {
        return canton.ToString().ToUpper();
    }

    private static string FormatDate(DateTime date)
    {
        return date.ToString("yyyyMMdd");
    }
}
