using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Music.Data.Migrations
{
    public partial class AddArtistAlphamericName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Artists_Name",
                schema: "music",
                table: "Artists");

            migrationBuilder.AddColumn<string>(
                name: "AlphamericName",
                schema: "music",
                table: "Artists",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Artists_AlphamericName",
                schema: "music",
                table: "Artists",
                column: "AlphamericName",
                unique: true,
                filter: "[AlphamericName] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Artists_AlphamericName",
                schema: "music",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "AlphamericName",
                schema: "music",
                table: "Artists");

            migrationBuilder.CreateIndex(
                name: "IX_Artists_Name",
                schema: "music",
                table: "Artists",
                column: "Name",
                unique: true,
                filter: "[Name] IS NOT NULL");
        }
    }
}
