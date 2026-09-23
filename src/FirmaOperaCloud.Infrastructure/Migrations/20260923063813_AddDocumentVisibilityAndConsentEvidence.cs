using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirmaOperaCloud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentVisibilityAndConsentEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConsentAcceptedAtUtc",
                table: "LocalDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsentTextVersion",
                table: "LocalDocuments",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HiddenAtUtc",
                table: "LocalDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HiddenBy",
                table: "LocalDocuments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HiddenReason",
                table: "LocalDocuments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "LocalDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MarketingConsent",
                table: "LocalDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SignatureAuthorizationAccepted",
                table: "LocalDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsentAcceptedAtUtc",
                table: "LocalDocuments");

            migrationBuilder.DropColumn(
                name: "ConsentTextVersion",
                table: "LocalDocuments");

            migrationBuilder.DropColumn(
                name: "HiddenAtUtc",
                table: "LocalDocuments");

            migrationBuilder.DropColumn(
                name: "HiddenBy",
                table: "LocalDocuments");

            migrationBuilder.DropColumn(
                name: "HiddenReason",
                table: "LocalDocuments");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "LocalDocuments");

            migrationBuilder.DropColumn(
                name: "MarketingConsent",
                table: "LocalDocuments");

            migrationBuilder.DropColumn(
                name: "SignatureAuthorizationAccepted",
                table: "LocalDocuments");
        }
    }
}
