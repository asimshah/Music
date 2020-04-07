using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Music.Data.Migrations
{
    public partial class ConvertArtistWorkToManyToMany : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Works_Artists_ArtistId",
                schema: "music",
                table: "Works");

            migrationBuilder.DropIndex(
                name: "IX_Works_ArtistId",
                schema: "music",
                table: "Works");

            migrationBuilder.CreateTable(
                name: "ArtistWorkList",
                schema: "music",
                columns: table => new
                {
                    ArtistId = table.Column<long>(nullable: false),
                    WorkId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtistWorkList", x => new { x.ArtistId, x.WorkId });
                    table.ForeignKey(
                        name: "FK_ArtistWorkList_Artists_ArtistId",
                        column: x => x.ArtistId,
                        principalSchema: "music",
                        principalTable: "Artists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArtistWorkList_Works_WorkId",
                        column: x => x.WorkId,
                        principalSchema: "music",
                        principalTable: "Works",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArtistWorkList_WorkId",
                schema: "music",
                table: "ArtistWorkList",
                column: "WorkId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArtistWorkList",
                schema: "music");

            migrationBuilder.CreateIndex(
                name: "IX_Works_ArtistId",
                schema: "music",
                table: "Works",
                column: "ArtistId");

            migrationBuilder.AddForeignKey(
                name: "FK_Works_Artists_ArtistId",
                schema: "music",
                table: "Works",
                column: "ArtistId",
                principalSchema: "music",
                principalTable: "Artists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
