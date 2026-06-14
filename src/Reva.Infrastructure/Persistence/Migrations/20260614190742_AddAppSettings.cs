using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reva.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Theme = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    AccentColor = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ConfidenceLowMax = table.Column<double>(type: "REAL", nullable: false),
                    ConfidenceMediumMax = table.Column<double>(type: "REAL", nullable: false),
                    DefaultTemplateId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");
        }
    }
}
