using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reva.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchemaMappings : Migration
    {
        private static readonly string[] LearnedSchemaMappingKey = ["SenderKey", "NormalizedSourceHeader"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentSchemaMappingRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SenderKey = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    SourceHeader = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    NormalizedSourceHeader = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    CanonicalField = table.Column<string>(type: "TEXT", maxLength: 96, nullable: false),
                    NormalizedValue = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Confidence = table.Column<double>(type: "REAL", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    IsLearned = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCorrected = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentSchemaMappingRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentSchemaMappingRecord_Documents_DocumentRecordId",
                        column: x => x.DocumentRecordId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearnedSchemaMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SenderKey = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    SourceHeader = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    NormalizedSourceHeader = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    CanonicalField = table.Column<string>(type: "TEXT", maxLength: 96, nullable: false),
                    UseCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnedSchemaMappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentSchemaMappingRecord_DocumentRecordId",
                table: "DocumentSchemaMappingRecord",
                column: "DocumentRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnedSchemaMappings_SenderKey_NormalizedSourceHeader",
                table: "LearnedSchemaMappings",
                columns: LearnedSchemaMappingKey,
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentSchemaMappingRecord");

            migrationBuilder.DropTable(
                name: "LearnedSchemaMappings");
        }
    }
}
