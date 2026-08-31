// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddECountingCerts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CertificateChains",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RawPem = table.Column<string>(type: "text", nullable: false),
                CantonSettingsId = table.Column<Guid>(type: "uuid", nullable: false),
                ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ImportedBy_FirstName = table.Column<string>(type: "text", nullable: false),
                ImportedBy_LastName = table.Column<string>(type: "text", nullable: false),
                ImportedBy_SecureConnectId = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CertificateChains", x => x.Id);
                table.ForeignKey(
                    name: "FK_CertificateChains_CantonSettings_CantonSettingsId",
                    column: x => x.CantonSettingsId,
                    principalTable: "CantonSettings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CertificateInfos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RawPem = table.Column<string>(type: "text", nullable: false),
                CertificateChainId = table.Column<Guid>(type: "uuid", nullable: false),
                NotBefore = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                NotAfter = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Issuer = table.Column<string>(type: "text", nullable: false),
                Subject = table.Column<string>(type: "text", nullable: false),
                Thumbprint = table.Column<string>(type: "text", nullable: false),
                CaThumbprint = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CertificateInfos", x => x.Id);
                table.ForeignKey(
                    name: "FK_CertificateInfos_CertificateChains_CertificateChainId",
                    column: x => x.CertificateChainId,
                    principalTable: "CertificateChains",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CertificateChains_CantonSettingsId",
            table: "CertificateChains",
            column: "CantonSettingsId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CertificateInfos_CertificateChainId",
            table: "CertificateInfos",
            column: "CertificateChainId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CertificateInfos");

        migrationBuilder.DropTable(
            name: "CertificateChains");
    }
}
