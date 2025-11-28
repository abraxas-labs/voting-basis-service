// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using CsvHelper.Configuration.Attributes;
using Voting.Basis.Core.Export.Generators.Csv.Converters;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Export.Generators.Csv;

public class CandidateListEntry
{
    [Name("Wahlbezeichnung")]
    public string? ElectionName { get; set; }

    [Name("Wahlbezeichnung kurz")]
    public string? ElectionNameShort { get; set; }

    [Name("Wahlkreis Bezeichnung")]
    public string? DomainOfInfluenceName { get; set; }

    [Name("Wahlkreis Kurzbezeichnung")]
    public string? DomainOfInfluenceNameShort { get; set; }

    [Name("Wahlkreis Bezeichnung für Protokoll")]
    public string? DomainOfInfluenceNameForProtocol { get; set; }

    [Name("Wahlkreis interne Bezeichnung")]
    public string? DomainOfInfluenceInternalDescription { get; set; }

    [Name("Geschäfts-ID")]
    public Guid PoliticalBusinessId { get; set; }

    [Name("Geschäfts-Parent-ID")]
    public Guid? PoliticalUnionId { get; set; }

    [Name("Listennummer")]
    public string? ListNumber { get; set; }

    [Name("Kandidierendennummer")]
    public string? Number { get; set; }

    [Name("Prüfziffer")]
    public int? CheckDigit { get; set; }

    [Name("Listenplatz")]
    public int Position { get; set; }

    [Name("Politischer Nachname")]
    public string? PoliticalLastName { get; set; }

    [Name("Politischer Vorname")]
    public string? PoliticalFirstName { get; set; }

    [Name("Amtlicher Nachname")]
    public string? LastName { get; set; }

    [Name("Amtlicher Vorname")]
    public string? FirstName { get; set; }

    [Name("Land")]
    public string? Country { get; set; }

    [Name("Geburtsdatum")]
    [TypeConverter(typeof(DateConverter))]
    public DateTime? DateOfBirth { get; set; }

    [Name("Geburtsjahr")]
    public int? YearOfBirth { get; set; }

    [Name("Heimatort")]
    public string? Origin { get; set; }

    [Name("Titel")]
    public string? Title { get; set; }

    [Name("Beruf")]
    public string? Occupation { get; set; }

    [Name("Strasse/Adresse")]
    public string? Street { get; set; }

    [Name("PLZ")]
    public string? ZipCode { get; set; }

    [Name("Wohnort")]
    public string? Locality { get; set; }

    [Name("Geschlecht")]
    [TypeConverter(typeof(SexConverter))]
    public SexType Gender { get; set; }

    [Name("Bisher")]
    [TypeConverter(typeof(BooleanConverter))]
    public bool Incumbent { get; set; }

    [Name("Vorkumuliert")]
    [TypeConverter(typeof(BooleanConverter))]
    public bool Accumulated { get; set; }

    [Name("Partei")]
    public string? Party { get; set; }

    [Name("Kontrolliert")]
    [TypeConverter(typeof(BooleanConverter))]
    public bool Audited { get; set; }

    [Name("Bezeichnung Wahlvorschlag")]
    public string? WahlvorschlagDescription { get; set; }

    [Name("Bemerkungen")]
    public string? Comment { get; set; }

    [Name("Listenbezeichnung")]
    public string? ListDescription { get; set; }

    [Name("Listenbezeichnung kurz")]
    public string? ListDescriptionShort { get; set; }

    [Name("LV")]
    public string? ListUnionName { get; set; }

    [Name("LUV")]
    public string? ListSubUnionName { get; set; }
}
