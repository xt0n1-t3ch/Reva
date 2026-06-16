using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reva.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSettingsDepthColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ReconciliationTolerance",
                table: "AppSettings",
                type: "REAL",
                nullable: false,
                defaultValue: 0.01);

            migrationBuilder.AddColumn<bool>(
                name: "UseLlmAssist",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReconciliationTolerance",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "UseLlmAssist",
                table: "AppSettings");
        }
    }
}
