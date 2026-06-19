using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Reva.Infrastructure.Persistence;

#nullable disable

namespace Reva.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(RevaDbContext))]
    [Migration("20260619173000_AddAiSettingsColumns")]
    public partial class AddAiSettingsColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiProvider",
                table: "AppSettings",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "Ollama");

            migrationBuilder.AddColumn<string>(
                name: "AiBaseUrl",
                table: "AppSettings",
                type: "TEXT",
                maxLength: 512,
                nullable: false,
                defaultValue: "http://localhost:11434/v1");

            migrationBuilder.AddColumn<string>(
                name: "AiApiKey",
                table: "AppSettings",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiModel",
                table: "AppSettings",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                defaultValue: "qwen2.5vl:7b");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiProvider",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "AiBaseUrl",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "AiApiKey",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "AiModel",
                table: "AppSettings");
        }
    }
}
