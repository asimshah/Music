using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Music.Data.Migrations
{
    public partial class RenameTrackExtendedTitle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtendedTitle",
                schema: "music",
                table: "Tracks");

            migrationBuilder.AddColumn<string>(
                name: "CompositionName",
                schema: "music",
                table: "Tracks",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompositionName",
                schema: "music",
                table: "Tracks");

            migrationBuilder.AddColumn<string>(
                name: "ExtendedTitle",
                schema: "music",
                table: "Tracks",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
        }
    }
}
