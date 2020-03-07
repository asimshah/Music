using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Music.Data.Migrations
{
    public partial class ReviseCompositionAlphamericIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Compositions_AlphamericName",
                schema: "music",
                table: "Compositions");

            migrationBuilder.DropIndex(
                name: "IX_Compositions_ArtistId",
                schema: "music",
                table: "Compositions");

            migrationBuilder.CreateIndex(
                name: "IX_Compositions_ArtistId_AlphamericName",
                schema: "music",
                table: "Compositions",
                columns: new[] { "ArtistId", "AlphamericName" },
                unique: true,
                filter: "[AlphamericName] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Compositions_ArtistId_AlphamericName",
                schema: "music",
                table: "Compositions");

            migrationBuilder.CreateIndex(
                name: "IX_Compositions_AlphamericName",
                schema: "music",
                table: "Compositions",
                column: "AlphamericName");

            migrationBuilder.CreateIndex(
                name: "IX_Compositions_ArtistId",
                schema: "music",
                table: "Compositions",
                column: "ArtistId");
        }
    }
}
