using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Music.Data.Migrations
{
    public partial class AddArtistSourceFlagToTaskItem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessingId",
                schema: "music",
                table: "TaskItems");

            migrationBuilder.AddColumn<bool>(
                name: "TaskStringIsArtistFolder",
                schema: "music",
                table: "TaskItems",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaskStringIsArtistFolder",
                schema: "music",
                table: "TaskItems");

            migrationBuilder.AddColumn<Guid>(
                name: "ProcessingId",
                schema: "music",
                table: "TaskItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
