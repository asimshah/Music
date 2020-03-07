using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Music.Data.Migrations
{
    public partial class AddAlphamericColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Works_Name",
                schema: "music",
                table: "Works");

            migrationBuilder.DropIndex(
                name: "IX_Tracks_Title",
                schema: "music",
                table: "Tracks");

            migrationBuilder.DropIndex(
                name: "IX_Compositions_Name",
                schema: "music",
                table: "Compositions");

            migrationBuilder.AddColumn<string>(
                name: "AlphamericName",
                schema: "music",
                table: "Works",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlphamericTitle",
                schema: "music",
                table: "Tracks",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlphamericPerformers",
                schema: "music",
                table: "Performances",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlphamericName",
                schema: "music",
                table: "Compositions",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Works_AlphamericName",
                schema: "music",
                table: "Works",
                column: "AlphamericName");

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_AlphamericTitle",
                schema: "music",
                table: "Tracks",
                column: "AlphamericTitle");

            migrationBuilder.CreateIndex(
                name: "IX_Performances_AlphamericPerformers",
                schema: "music",
                table: "Performances",
                column: "AlphamericPerformers");

            migrationBuilder.CreateIndex(
                name: "IX_Compositions_AlphamericName",
                schema: "music",
                table: "Compositions",
                column: "AlphamericName");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Works_AlphamericName",
                schema: "music",
                table: "Works");

            migrationBuilder.DropIndex(
                name: "IX_Tracks_AlphamericTitle",
                schema: "music",
                table: "Tracks");

            migrationBuilder.DropIndex(
                name: "IX_Performances_AlphamericPerformers",
                schema: "music",
                table: "Performances");

            migrationBuilder.DropIndex(
                name: "IX_Compositions_AlphamericName",
                schema: "music",
                table: "Compositions");

            migrationBuilder.DropColumn(
                name: "AlphamericName",
                schema: "music",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "AlphamericTitle",
                schema: "music",
                table: "Tracks");

            migrationBuilder.DropColumn(
                name: "AlphamericPerformers",
                schema: "music",
                table: "Performances");

            migrationBuilder.DropColumn(
                name: "AlphamericName",
                schema: "music",
                table: "Compositions");

            migrationBuilder.CreateIndex(
                name: "IX_Works_Name",
                schema: "music",
                table: "Works",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_Title",
                schema: "music",
                table: "Tracks",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Compositions_Name",
                schema: "music",
                table: "Compositions",
                column: "Name");
        }
    }
}
