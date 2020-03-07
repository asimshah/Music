using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Music.Data.Migrations
{
    public partial class ChangeMBIDToUID : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MBID",
                schema: "music",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "MBID",
                schema: "music",
                table: "Tracks");

            migrationBuilder.DropColumn(
                name: "MBID",
                schema: "music",
                table: "Artists");

            migrationBuilder.AddColumn<Guid>(
                name: "UID",
                schema: "music",
                table: "Works",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UID",
                schema: "music",
                table: "Tracks",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UID",
                schema: "music",
                table: "Artists",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UID",
                schema: "music",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "UID",
                schema: "music",
                table: "Tracks");

            migrationBuilder.DropColumn(
                name: "UID",
                schema: "music",
                table: "Artists");

            migrationBuilder.AddColumn<Guid>(
                name: "MBID",
                schema: "music",
                table: "Works",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MBID",
                schema: "music",
                table: "Tracks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MBID",
                schema: "music",
                table: "Artists",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
