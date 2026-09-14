using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirmaOperaCloud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationCardDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistrationCardDrafts",
                columns: table => new
                {
                    HotelId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ConfirmationNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ReservationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationCardDrafts", x => new { x.HotelId, x.ConfirmationNumber, x.ReservationId });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrationCardDrafts");
        }
    }
}
