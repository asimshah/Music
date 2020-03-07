using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Music.Data.Migrations
{
    public partial class AddTaskItemMusicStyleAndForce : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Force",
                schema: "music",
                table: "TaskItems",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MusicStyle",
                schema: "music",
                table: "TaskItems",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Force",
                schema: "music",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "MusicStyle",
                schema: "music",
                table: "TaskItems");
        }
    }
}
