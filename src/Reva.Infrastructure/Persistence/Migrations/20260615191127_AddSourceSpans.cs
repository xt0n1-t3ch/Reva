using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reva.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceSpans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentPages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Page = table.Column<int>(type: "INTEGER", nullable: false),
                    ImagePath = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Width = table.Column<double>(type: "REAL", nullable: false),
                    Height = table.Column<double>(type: "REAL", nullable: false),
                    Rotation = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentPages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentPages_Documents_DocumentRecordId",
                        column: x => x.DocumentRecordId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentSourceSpans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SpanId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Page = table.Column<int>(type: "INTEGER", nullable: false),
                    PageWidth = table.Column<double>(type: "REAL", nullable: false),
                    PageHeight = table.Column<double>(type: "REAL", nullable: false),
                    Rotation = table.Column<int>(type: "INTEGER", nullable: false),
                    X = table.Column<double>(type: "REAL", nullable: false),
                    Y = table.Column<double>(type: "REAL", nullable: false),
                    Width = table.Column<double>(type: "REAL", nullable: false),
                    Height = table.Column<double>(type: "REAL", nullable: false),
                    PolygonJson = table.Column<string>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    OcrConfidence = table.Column<double>(type: "REAL", nullable: true),
                    BlockId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    TableId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    RowIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    ColumnIndex = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentSourceSpans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentSourceSpans_Documents_DocumentRecordId",
                        column: x => x.DocumentRecordId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentPages_DocumentRecordId",
                table: "DocumentPages",
                column: "DocumentRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentSourceSpans_DocumentRecordId",
                table: "DocumentSourceSpans",
                column: "DocumentRecordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentPages");

            migrationBuilder.DropTable(
                name: "DocumentSourceSpans");
        }
    }
}
