using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Music.Data.Migrations
{
    public partial class ChangeTaskItemAddForSingles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaskStringIsArtistFolder",
                schema: "music",
                table: "TaskItems");

            migrationBuilder.AddColumn<bool>(
                name: "ForSingles",
                schema: "music",
                table: "TaskItems",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ForSingles",
                schema: "music",
                table: "TaskItems");

            migrationBuilder.AddColumn<bool>(
                name: "TaskStringIsArtistFolder",
                schema: "music",
                table: "TaskItems",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
