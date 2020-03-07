using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Music.Data.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "music");

            migrationBuilder.CreateTable(
                name: "Artists",
                schema: "music",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MBID = table.Column<Guid>(nullable: true),
                    Name = table.Column<string>(maxLength: 128, nullable: true),
                    CompressedName = table.Column<string>(maxLength: 16, nullable: true),
                    Type = table.Column<int>(nullable: false),
                    OriginalName = table.Column<string>(maxLength: 128, nullable: true),
                    MbidName = table.Column<string>(maxLength: 128, nullable: true),
                    IdTagName = table.Column<string>(maxLength: 128, nullable: true),
                    UserProvidedName = table.Column<string>(maxLength: 128, nullable: true),
                    ParsingStage = table.Column<int>(nullable: false),
                    ImageChecksum = table.Column<long>(nullable: false),
                    ImageData = table.Column<byte[]>(nullable: true),
                    ImageMimeType = table.Column<string>(maxLength: 64, nullable: true),
                    ImageDateTime = table.Column<DateTimeOffset>(nullable: false),
                    HasDefaultImage = table.Column<bool>(nullable: false),
                    LastModified = table.Column<DateTimeOffset>(nullable: false),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Playlists",
                schema: "music",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    ModificationUid = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Playlists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskItems",
                schema: "music",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<int>(nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(nullable: false),
                    TargetPath = table.Column<string>(nullable: true),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArtistStyles",
                schema: "music",
                columns: table => new
                {
                    ArtistId = table.Column<long>(nullable: false),
                    StyleId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtistStyles", x => new { x.ArtistId, x.StyleId });
                    table.ForeignKey(
                        name: "FK_ArtistStyles_Artists_ArtistId",
                        column: x => x.ArtistId,
                        principalSchema: "music",
                        principalTable: "Artists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Compositions",
                schema: "music",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(maxLength: 512, nullable: false),
                    CompressedName = table.Column<string>(maxLength: 16, nullable: true),
                    ArtistId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Compositions_Artists_ArtistId",
                        column: x => x.ArtistId,
                        principalSchema: "music",
                        principalTable: "Artists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Works",
                schema: "music",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MBID = table.Column<Guid>(nullable: true),
                    Name = table.Column<string>(maxLength: 128, nullable: false),
                    CompressedName = table.Column<string>(maxLength: 16, nullable: true),
                    IsMultiPart = table.Column<bool>(nullable: false),
                    PartNumber = table.Column<int>(nullable: false),
                    PartName = table.Column<string>(maxLength: 64, nullable: true),
                    Type = table.Column<int>(nullable: false),
                    OriginalName = table.Column<string>(maxLength: 128, nullable: false),
                    MbidName = table.Column<string>(maxLength: 128, nullable: true),
                    IdTagName = table.Column<string>(maxLength: 128, nullable: true),
                    UserProvidedName = table.Column<string>(maxLength: 128, nullable: true),
                    DisambiguationName = table.Column<string>(maxLength: 128, nullable: true),
                    ParsingStage = table.Column<int>(nullable: false),
                    StyleId = table.Column<int>(nullable: false),
                    ArtistId = table.Column<long>(nullable: false),
                    HasDefaultCover = table.Column<bool>(nullable: false),
                    Mood = table.Column<string>(nullable: true),
                    Year = table.Column<int>(nullable: false),
                    CoverChecksum = table.Column<long>(nullable: false),
                    CoverData = table.Column<byte[]>(nullable: true),
                    CoverMimeType = table.Column<string>(maxLength: 64, nullable: true),
                    CoverDateTime = table.Column<DateTimeOffset>(nullable: false),
                    LastModified = table.Column<DateTimeOffset>(nullable: false),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Works", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Works_Artists_ArtistId",
                        column: x => x.ArtistId,
                        principalSchema: "music",
                        principalTable: "Artists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                schema: "music",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KeyName = table.Column<string>(nullable: true),
                    DisplayName = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    Type = table.Column<int>(nullable: false),
                    HostMachine = table.Column<string>(nullable: true),
                    PlayerUrl = table.Column<string>(nullable: true),
                    MACAddress = table.Column<string>(nullable: true),
                    IsDefaultOnHost = table.Column<bool>(nullable: false),
                    IsDisabled = table.Column<bool>(nullable: false),
                    CanReposition = table.Column<bool>(nullable: false),
                    MaxSampleRate = table.Column<int>(nullable: false),
                    LastSeenDateTime = table.Column<DateTimeOffset>(nullable: false),
                    Volume = table.Column<float>(nullable: false),
                    PlaylistId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devices_Playlists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalSchema: "music",
                        principalTable: "Playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaylistItems",
                schema: "music",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(nullable: false),
                    Title = table.Column<string>(nullable: true),
                    Sequence = table.Column<int>(nullable: false),
                    ItemId = table.Column<long>(nullable: false),
                    MusicFileId = table.Column<long>(nullable: false),
                    PlaylistId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaylistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaylistItems_Playlists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalSchema: "music",
                        principalTable: "Playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Performances",
                schema: "music",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompositionId = table.Column<long>(nullable: false),
                    Year = table.Column<int>(nullable: false),
                    Performers = table.Column<string>(maxLength: 2048, nullable: true),
                    CompressedName = table.Column<string>(maxLength: 16, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Performances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Performances_Compositions_CompositionId",
                        column: x => x.CompositionId,
                        principalSchema: "music",
                        principalTable: "Compositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tracks",
                schema: "music",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<int>(nullable: false),
                    MovementNumber = table.Column<int>(nullable: false),
                    Title = table.Column<string>(maxLength: 256, nullable: false),
                    CompressedName = table.Column<string>(maxLength: 16, nullable: true),
                    ExtendedTitle = table.Column<string>(maxLength: 256, nullable: false),
                    OriginalTitle = table.Column<string>(maxLength: 256, nullable: false),
                    MbidName = table.Column<string>(nullable: true),
                    IdTagName = table.Column<string>(maxLength: 256, nullable: true),
                    UserProvidedName = table.Column<string>(nullable: true),
                    MBID = table.Column<Guid>(nullable: true),
                    NumberParsingStage = table.Column<int>(nullable: false),
                    ParsingStage = table.Column<int>(nullable: false),
                    WorkId = table.Column<long>(nullable: false),
                    PerformanceId = table.Column<long>(nullable: true),
                    LastModified = table.Column<DateTimeOffset>(nullable: false),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tracks_Performances_PerformanceId",
                        column: x => x.PerformanceId,
                        principalSchema: "music",
                        principalTable: "Performances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tracks_Works_WorkId",
                        column: x => x.WorkId,
                        principalSchema: "music",
                        principalTable: "Works",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MusicFiles",
                schema: "music",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Style = table.Column<int>(nullable: false),
                    DiskRoot = table.Column<string>(maxLength: 256, nullable: true),
                    StylePath = table.Column<string>(maxLength: 256, nullable: true),
                    OpusPath = table.Column<string>(maxLength: 512, nullable: true),
                    File = table.Column<string>(maxLength: 2048, nullable: true),
                    FileLength = table.Column<long>(nullable: false),
                    FileLastWriteTimeUtc = table.Column<DateTimeOffset>(nullable: false),
                    Uid = table.Column<string>(maxLength: 36, nullable: true),
                    Musician = table.Column<string>(maxLength: 128, nullable: true),
                    MusicianType = table.Column<int>(nullable: false),
                    OpusType = table.Column<int>(nullable: false),
                    IsMultiPart = table.Column<bool>(nullable: false),
                    OpusName = table.Column<string>(maxLength: 128, nullable: true),
                    PartName = table.Column<string>(maxLength: 64, nullable: true),
                    PartNumber = table.Column<int>(nullable: false),
                    Encoding = table.Column<int>(nullable: false),
                    Mode = table.Column<int>(nullable: false),
                    Duration = table.Column<double>(nullable: true),
                    BitsPerSample = table.Column<int>(nullable: true),
                    SampleRate = table.Column<int>(nullable: true),
                    MinimumBitRate = table.Column<int>(nullable: true),
                    MaximumBitRate = table.Column<int>(nullable: true),
                    AverageBitRate = table.Column<double>(nullable: true),
                    IsGenerated = table.Column<bool>(nullable: false),
                    IsFaulty = table.Column<bool>(nullable: false),
                    LastPlayedAt = table.Column<DateTimeOffset>(nullable: false),
                    LastCataloguedAt = table.Column<DateTimeOffset>(nullable: false),
                    ParsingStage = table.Column<int>(nullable: false),
                    Mood = table.Column<string>(nullable: true),
                    TrackId = table.Column<long>(nullable: true),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MusicFiles_Tracks_TrackId",
                        column: x => x.TrackId,
                        principalSchema: "music",
                        principalTable: "Tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdTags",
                schema: "music",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(maxLength: 64, nullable: true),
                    Value = table.Column<string>(nullable: true),
                    PictureChecksum = table.Column<long>(nullable: false),
                    PictureData = table.Column<byte[]>(nullable: true),
                    PictureMimeType = table.Column<string>(maxLength: 64, nullable: true),
                    MusicFileId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdTags_MusicFiles_MusicFileId",
                        column: x => x.MusicFileId,
                        principalSchema: "music",
                        principalTable: "MusicFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Artists_Name",
                schema: "music",
                table: "Artists",
                column: "Name",
                unique: true,
                filter: "[Name] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Compositions_ArtistId",
                schema: "music",
                table: "Compositions",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_Compositions_Name",
                schema: "music",
                table: "Compositions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_PlaylistId",
                schema: "music",
                table: "Devices",
                column: "PlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_IdTags_Name",
                schema: "music",
                table: "IdTags",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_IdTags_MusicFileId_Name",
                schema: "music",
                table: "IdTags",
                columns: new[] { "MusicFileId", "Name" },
                unique: true,
                filter: "[Name] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MusicFiles_DiskRoot",
                schema: "music",
                table: "MusicFiles",
                column: "DiskRoot");

            migrationBuilder.CreateIndex(
                name: "IX_MusicFiles_File",
                schema: "music",
                table: "MusicFiles",
                column: "File",
                unique: true,
                filter: "[File] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MusicFiles_OpusPath",
                schema: "music",
                table: "MusicFiles",
                column: "OpusPath");

            migrationBuilder.CreateIndex(
                name: "IX_MusicFiles_Style",
                schema: "music",
                table: "MusicFiles",
                column: "Style");

            migrationBuilder.CreateIndex(
                name: "IX_MusicFiles_StylePath",
                schema: "music",
                table: "MusicFiles",
                column: "StylePath");

            migrationBuilder.CreateIndex(
                name: "IX_MusicFiles_TrackId",
                schema: "music",
                table: "MusicFiles",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicFiles_Uid",
                schema: "music",
                table: "MusicFiles",
                column: "Uid",
                unique: true,
                filter: "[Uid] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Performances_CompositionId",
                schema: "music",
                table: "Performances",
                column: "CompositionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistItems_PlaylistId",
                schema: "music",
                table: "PlaylistItems",
                column: "PlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_ScheduledAt",
                schema: "music",
                table: "TaskItems",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_Status",
                schema: "music",
                table: "TaskItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_TargetPath",
                schema: "music",
                table: "TaskItems",
                column: "TargetPath");

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_PerformanceId",
                schema: "music",
                table: "Tracks",
                column: "PerformanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_Title",
                schema: "music",
                table: "Tracks",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_WorkId",
                schema: "music",
                table: "Tracks",
                column: "WorkId");

            migrationBuilder.CreateIndex(
                name: "IX_Works_ArtistId",
                schema: "music",
                table: "Works",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_Works_Name",
                schema: "music",
                table: "Works",
                column: "Name");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArtistStyles",
                schema: "music");

            migrationBuilder.DropTable(
                name: "Devices",
                schema: "music");

            migrationBuilder.DropTable(
                name: "IdTags",
                schema: "music");

            migrationBuilder.DropTable(
                name: "PlaylistItems",
                schema: "music");

            migrationBuilder.DropTable(
                name: "TaskItems",
                schema: "music");

            migrationBuilder.DropTable(
                name: "MusicFiles",
                schema: "music");

            migrationBuilder.DropTable(
                name: "Playlists",
                schema: "music");

            migrationBuilder.DropTable(
                name: "Tracks",
                schema: "music");

            migrationBuilder.DropTable(
                name: "Performances",
                schema: "music");

            migrationBuilder.DropTable(
                name: "Works",
                schema: "music");

            migrationBuilder.DropTable(
                name: "Compositions",
                schema: "music");

            migrationBuilder.DropTable(
                name: "Artists",
                schema: "music");
        }
    }
}
