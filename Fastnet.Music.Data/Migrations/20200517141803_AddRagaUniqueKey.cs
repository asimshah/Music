using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Music.Data.Migrations
{
    public partial class AddRagaUniqueKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Ragas_AlphamericName",
                schema: "music",
                table: "Ragas",
                column: "AlphamericName",
                unique: true,
                filter: "[AlphamericName] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ragas_AlphamericName",
                schema: "music",
                table: "Ragas");
        }
    }
}
