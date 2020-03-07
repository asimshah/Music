using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Music.Data.Migrations
{
    public partial class AddOwnedImage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverChecksum",
                schema: "music",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "CoverData",
                schema: "music",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "CoverDateTime",
                schema: "music",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "CoverMimeType",
                schema: "music",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "HasDefaultCover",
                schema: "music",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "HasDefaultImage",
                schema: "music",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "ImageChecksum",
                schema: "music",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "ImageData",
                schema: "music",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "ImageDateTime",
                schema: "music",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "ImageMimeType",
                schema: "music",
                table: "Artists");

            migrationBuilder.AddColumn<byte[]>(
                name: "Cover_Data",
                schema: "music",
                table: "Works",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Cover_Filelength",
                schema: "music",
                table: "Works",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Cover_LastModified",
                schema: "music",
                table: "Works",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cover_MimeType",
                schema: "music",
                table: "Works",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cover_Sourcefile",
                schema: "music",
                table: "Works",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Portrait_Data",
                schema: "music",
                table: "Artists",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Portrait_Filelength",
                schema: "music",
                table: "Artists",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Portrait_LastModified",
                schema: "music",
                table: "Artists",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Portrait_MimeType",
                schema: "music",
                table: "Artists",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Portrait_Sourcefile",
                schema: "music",
                table: "Artists",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cover_Data",
                schema: "music",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "Cover_Filelength",
                schema: "music",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "Cover_LastModified",
                schema: "music",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "Cover_MimeType",
                schema: "music",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "Cover_Sourcefile",
                schema: "music",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "Portrait_Data",
                schema: "music",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "Portrait_Filelength",
                schema: "music",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "Portrait_LastModified",
                schema: "music",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "Portrait_MimeType",
                schema: "music",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "Portrait_Sourcefile",
                schema: "music",
                table: "Artists");

            migrationBuilder.AddColumn<long>(
                name: "CoverChecksum",
                schema: "music",
                table: "Works",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<byte[]>(
                name: "CoverData",
                schema: "music",
                table: "Works",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CoverDateTime",
                schema: "music",
                table: "Works",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CoverMimeType",
                schema: "music",
                table: "Works",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasDefaultCover",
                schema: "music",
                table: "Works",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasDefaultImage",
                schema: "music",
                table: "Artists",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "ImageChecksum",
                schema: "music",
                table: "Artists",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageData",
                schema: "music",
                table: "Artists",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ImageDateTime",
                schema: "music",
                table: "Artists",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "ImageMimeType",
                schema: "music",
                table: "Artists",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }
    }
}
