using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reva.Infrastructure.Persistence.Migrations
{
    public partial class AddSchemaMappingOverrideColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Confidence",
                table: "LearnedSchemaMappings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOverride",
                table: "LearnedSchemaMappings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "LearnedSchemaMappings");

            migrationBuilder.DropColumn(
                name: "IsOverride",
                table: "LearnedSchemaMappings");
        }
    }
}
