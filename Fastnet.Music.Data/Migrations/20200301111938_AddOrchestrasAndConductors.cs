using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Music.Data.Migrations
{
    public partial class AddOrchestrasAndConductors : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Conductors",
                schema: "music",
                table: "Performances",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Orchestras",
                schema: "music",
                table: "Performances",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Conductors",
                schema: "music",
                table: "Performances");

            migrationBuilder.DropColumn(
                name: "Orchestras",
                schema: "music",
                table: "Performances");
        }
    }
}
