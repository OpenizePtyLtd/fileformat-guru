using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileFormatAIStudio.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatSessionAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatSessionAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    FileType = table.Column<string>(type: "TEXT", nullable: false),
                    DocumentCategory = table.Column<string>(type: "TEXT", nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ParserEngineUsed = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    RawExtractedText = table.Column<string>(type: "TEXT", nullable: true),
                    TokenCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ChunkCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AttachedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSessionAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatSessionAttachments_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessionAttachments_SessionId",
                table: "ChatSessionAttachments",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatSessionAttachments");
        }
    }
}
