using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Music.Data.Migrations
{
    public partial class AddTaskItemTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskItems_TargetPath",
                schema: "music",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "TargetPath",
                schema: "music",
                table: "TaskItems");

            migrationBuilder.AddColumn<string>(
                name: "TaskString",
                schema: "music",
                table: "TaskItems",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                schema: "music",
                table: "TaskItems",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaskString",
                schema: "music",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "music",
                table: "TaskItems");

            migrationBuilder.AddColumn<string>(
                name: "TargetPath",
                schema: "music",
                table: "TaskItems",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_TargetPath",
                schema: "music",
                table: "TaskItems",
                column: "TargetPath");
        }
    }
}
