using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Music.Data.Migrations
{
    public partial class AddLastModifiedToPlaylist2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModificationUid",
                schema: "music",
                table: "Playlists");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastModified",
                schema: "music",
                table: "Playlists",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastModified",
                schema: "music",
                table: "Playlists");

            migrationBuilder.AddColumn<string>(
                name: "ModificationUid",
                schema: "music",
                table: "Playlists",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
