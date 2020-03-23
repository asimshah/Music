using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Music.Data.Migrations
{
    public partial class MakePerformerTypeAndNameUnique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Performers_Type_Name",
                schema: "music",
                table: "Performers",
                columns: new[] { "Type", "Name" },
                unique: true,
                filter: "[Name] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Performers_Type_Name",
                schema: "music",
                table: "Performers");
        }
    }
}
