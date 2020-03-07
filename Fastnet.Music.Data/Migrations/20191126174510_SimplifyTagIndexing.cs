using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Music.Data.Migrations
{
    public partial class SimplifyTagIndexing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IdTags_MusicFileId_Name",
                schema: "music",
                table: "IdTags");

            migrationBuilder.CreateIndex(
                name: "IX_IdTags_MusicFileId",
                schema: "music",
                table: "IdTags",
                column: "MusicFileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IdTags_MusicFileId",
                schema: "music",
                table: "IdTags");

            migrationBuilder.CreateIndex(
                name: "IX_IdTags_MusicFileId_Name",
                schema: "music",
                table: "IdTags",
                columns: new[] { "MusicFileId", "Name" },
                unique: true,
                filter: "[Name] IS NOT NULL");
        }
    }
}
