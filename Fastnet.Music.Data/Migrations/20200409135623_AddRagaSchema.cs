using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Music.Data.Migrations
{
    public partial class AddRagaSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ragas",
                schema: "music",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(maxLength: 512, nullable: false),
                    AlphamericName = table.Column<string>(maxLength: 512, nullable: true),
                    CompressedName = table.Column<string>(maxLength: 16, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ragas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RagaPerformances",
                schema: "music",
                columns: table => new
                {
                    PerformanceId = table.Column<long>(nullable: false),
                    RagaId = table.Column<long>(nullable: false),
                    ArtistId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RagaPerformances", x => new { x.ArtistId, x.RagaId, x.PerformanceId });
                    table.ForeignKey(
                        name: "FK_RagaPerformances_Artists_ArtistId",
                        column: x => x.ArtistId,
                        principalSchema: "music",
                        principalTable: "Artists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RagaPerformances_Performances_PerformanceId",
                        column: x => x.PerformanceId,
                        principalSchema: "music",
                        principalTable: "Performances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RagaPerformances_Ragas_RagaId",
                        column: x => x.RagaId,
                        principalSchema: "music",
                        principalTable: "Ragas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RagaPerformances_PerformanceId",
                schema: "music",
                table: "RagaPerformances",
                column: "PerformanceId");

            migrationBuilder.CreateIndex(
                name: "IX_RagaPerformances_RagaId",
                schema: "music",
                table: "RagaPerformances",
                column: "RagaId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RagaPerformances",
                schema: "music");

            migrationBuilder.DropTable(
                name: "Ragas",
                schema: "music");
        }
    }
}
