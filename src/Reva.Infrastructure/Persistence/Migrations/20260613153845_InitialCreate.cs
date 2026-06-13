using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reva.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    Sha256Hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Extension = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    StoragePath = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ReviewState = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    DocumentType = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false),
                    Confidence = table.Column<double>(type: "REAL", nullable: false),
                    ParsedMarkdown = table.Column<string>(type: "TEXT", nullable: false),
                    ParsedJson = table.Column<string>(type: "TEXT", nullable: false),
                    ParserProfile = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentFieldRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 96, nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    Confidence = table.Column<double>(type: "REAL", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 96, nullable: false),
                    IsCorrected = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentFieldRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentFieldRecord_Documents_DocumentRecordId",
                        column: x => x.DocumentRecordId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentIssueRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentIssueRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentIssueRecord_Documents_DocumentRecordId",
                        column: x => x.DocumentRecordId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentTableRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 96, nullable: false),
                    HeadersJson = table.Column<string>(type: "TEXT", nullable: false),
                    RowsJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTableRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentTableRecord_Documents_DocumentRecordId",
                        column: x => x.DocumentRecordId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReviewEventRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Decision = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Reviewer = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewEventRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewEventRecord_Documents_DocumentRecordId",
                        column: x => x.DocumentRecordId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFieldRecord_DocumentRecordId",
                table: "DocumentFieldRecord",
                column: "DocumentRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentIssueRecord_DocumentRecordId",
                table: "DocumentIssueRecord",
                column: "DocumentRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Sha256Hash",
                table: "Documents",
                column: "Sha256Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTableRecord_DocumentRecordId",
                table: "DocumentTableRecord",
                column: "DocumentRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewEventRecord_DocumentRecordId",
                table: "ReviewEventRecord",
                column: "DocumentRecordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentFieldRecord");

            migrationBuilder.DropTable(
                name: "DocumentIssueRecord");

            migrationBuilder.DropTable(
                name: "DocumentTableRecord");

            migrationBuilder.DropTable(
                name: "ReviewEventRecord");

            migrationBuilder.DropTable(
                name: "Documents");
        }
    }
}
