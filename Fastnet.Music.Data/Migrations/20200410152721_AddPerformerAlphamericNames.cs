using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Music.Data.Migrations
{
    public partial class AddPerformerAlphamericNames : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Performers_Type_Name",
                schema: "music",
                table: "Performers");

            migrationBuilder.AddColumn<string>(
                name: "AlphamericName",
                schema: "music",
                table: "Performers",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Performers_AlphamericName_Type",
                schema: "music",
                table: "Performers",
                columns: new[] { "AlphamericName", "Type" },
                unique: true,
                filter: "[AlphamericName] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Performers_AlphamericName_Type",
                schema: "music",
                table: "Performers");

            migrationBuilder.DropColumn(
                name: "AlphamericName",
                schema: "music",
                table: "Performers");

            migrationBuilder.CreateIndex(
                name: "IX_Performers_Type_Name",
                schema: "music",
                table: "Performers",
                columns: new[] { "Type", "Name" },
                unique: true,
                filter: "[Name] IS NOT NULL");
        }
    }
}
