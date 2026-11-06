using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileFormatAIStudio.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFileFormatsCatalogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileFormats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Extension = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    CategoryDisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    DocsUrl = table.Column<string>(type: "TEXT", nullable: false),
                    IsSupported = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    AsposeApi = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultOssEngine = table.Column<string>(type: "TEXT", nullable: false),
                    PreferredEngineId = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileFormats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileFormats_Category",
                table: "FileFormats",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_FileFormats_Extension",
                table: "FileFormats",
                column: "Extension");

            migrationBuilder.CreateIndex(
                name: "IX_FileFormats_Extension_Category",
                table: "FileFormats",
                columns: new[] { "Extension", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileFormats_IsSupported",
                table: "FileFormats",
                column: "IsSupported");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileFormats");
        }
    }
}
