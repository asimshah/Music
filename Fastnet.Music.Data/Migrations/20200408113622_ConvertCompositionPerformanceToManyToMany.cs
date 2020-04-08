using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Music.Data.Migrations
{
    public partial class ConvertCompositionPerformanceToManyToMany : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Performances_Compositions_CompositionId",
                schema: "music",
                table: "Performances");

            migrationBuilder.DropIndex(
                name: "IX_Performances_CompositionId",
                schema: "music",
                table: "Performances");

            migrationBuilder.CreateTable(
                name: "CompositionPerformances",
                schema: "music",
                columns: table => new
                {
                    PerformanceId = table.Column<long>(nullable: false),
                    CompositionId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompositionPerformances", x => new { x.CompositionId, x.PerformanceId });
                    table.ForeignKey(
                        name: "FK_CompositionPerformances_Compositions_CompositionId",
                        column: x => x.CompositionId,
                        principalSchema: "music",
                        principalTable: "Compositions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompositionPerformances_Performances_PerformanceId",
                        column: x => x.PerformanceId,
                        principalSchema: "music",
                        principalTable: "Performances",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompositionPerformances_PerformanceId",
                schema: "music",
                table: "CompositionPerformances",
                column: "PerformanceId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompositionPerformances",
                schema: "music");

            migrationBuilder.CreateIndex(
                name: "IX_Performances_CompositionId",
                schema: "music",
                table: "Performances",
                column: "CompositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Performances_Compositions_CompositionId",
                schema: "music",
                table: "Performances",
                column: "CompositionId",
                principalSchema: "music",
                principalTable: "Compositions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
