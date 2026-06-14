using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reva.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReconciliationExceptionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Confidence",
                table: "DocumentIssueRecord",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Detected",
                table: "DocumentIssueRecord",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Expected",
                table: "DocumentIssueRecord",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FieldName",
                table: "DocumentIssueRecord",
                type: "TEXT",
                maxLength: 96,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "DocumentIssueRecord");

            migrationBuilder.DropColumn(
                name: "Detected",
                table: "DocumentIssueRecord");

            migrationBuilder.DropColumn(
                name: "Expected",
                table: "DocumentIssueRecord");

            migrationBuilder.DropColumn(
                name: "FieldName",
                table: "DocumentIssueRecord");
        }
    }
}
