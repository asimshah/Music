using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Music.Data.Migrations
{
    public partial class RefactorPerformers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Performers",
                schema: "music",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Performers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerformancePerformers",
                schema: "music",
                columns: table => new
                {
                    PerformanceId = table.Column<long>(nullable: false),
                    PerformerId = table.Column<long>(nullable: false),
                    Selected = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformancePerformers", x => new { x.PerformanceId, x.PerformerId });
                    table.ForeignKey(
                        name: "FK_PerformancePerformers_Performances_PerformanceId",
                        column: x => x.PerformanceId,
                        principalSchema: "music",
                        principalTable: "Performances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PerformancePerformers_Performers_PerformerId",
                        column: x => x.PerformerId,
                        principalSchema: "music",
                        principalTable: "Performers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerformancePerformers_PerformerId",
                schema: "music",
                table: "PerformancePerformers",
                column: "PerformerId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerformancePerformers",
                schema: "music");

            migrationBuilder.DropTable(
                name: "Performers",
                schema: "music");
        }
    }
}
