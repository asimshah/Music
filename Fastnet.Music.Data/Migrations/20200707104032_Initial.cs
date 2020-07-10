using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fastnet.Music.Data.Migrations
{
    public partial class Initial : Migration
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
                    UID = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 128, nullable: true),
                    Reputation = table.Column<int>(maxLength: 128, nullable: false),
                    AlphamericName = table.Column<string>(nullable: true),
                    CompressedName = table.Column<string>(maxLength: 16, nullable: true),
                    Type = table.Column<int>(nullable: false),
                    OriginalName = table.Column<string>(maxLength: 128, nullable: true),
                    MbidName = table.Column<string>(maxLength: 128, nullable: true),
                    IdTagName = table.Column<string>(maxLength: 128, nullable: true),
                    UserProvidedName = table.Column<string>(maxLength: 128, nullable: true),
                    ParsingStage = table.Column<int>(nullable: false),
                    LastModified = table.Column<DateTimeOffset>(nullable: false),
                    Portrait_Sourcefile = table.Column<string>(nullable: true),
                    Portrait_Filelength = table.Column<long>(nullable: true),
                    Portrait_LastModified = table.Column<DateTimeOffset>(nullable: true),
                    Portrait_MimeType = table.Column<string>(nullable: true),
                    Portrait_Data = table.Column<byte[]>(nullable: true),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Performances",
                schema: "music",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StyleId = table.Column<int>(nullable: false),
                    CompositionId = table.Column<long>(nullable: false),
                    Year = table.Column<int>(nullable: false),
                    Performers = table.Column<string>(maxLength: 2048, nullable: true),
                    Orchestras = table.Column<string>(nullable: true),
                    Conductors = table.Column<string>(nullable: true),
                    AlphamericPerformers = table.Column<string>(maxLength: 2048, nullable: true),
                    CompressedName = table.Column<string>(maxLength: 16, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Performances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Performers",
                schema: "music",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 128, nullable: true),
                    AlphamericName = table.Column<string>(maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Performers", x => x.Id);
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
                    LastModified = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Playlists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ragas",
                schema: "music",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(maxLength: 512, nullable: false),
                    DisplayName = table.Column<string>(nullable: true),
                    AlphamericName = table.Column<string>(maxLength: 512, nullable: true),
                    CompressedName = table.Column<string>(maxLength: 16, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ragas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskItems",
                schema: "music",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<int>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(nullable: false),
                    MusicStyle = table.Column<int>(nullable: false),
                    TaskString = table.Column<string>(nullable: true),
                    ForSingles = table.Column<bool>(nullable: false),
                    Force = table.Column<bool>(nullable: false),
                    RetryCount = table.Column<int>(nullable: false),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Works",
                schema: "music",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UID = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 256, nullable: false),
                    AlphamericName = table.Column<string>(maxLength: 256, nullable: true),
                    CompressedName = table.Column<string>(maxLength: 16, nullable: true),
                    IsMultiPart = table.Column<bool>(nullable: false),
                    PartNumber = table.Column<int>(nullable: false),
                    PartName = table.Column<string>(maxLength: 64, nullable: true),
                    Type = table.Column<int>(nullable: false),
                    OriginalName = table.Column<string>(maxLength: 256, nullable: false),
                    MbidName = table.Column<string>(maxLength: 128, nullable: true),
                    IdTagName = table.Column<string>(maxLength: 128, nullable: true),
                    UserProvidedName = table.Column<string>(maxLength: 128, nullable: true),
                    DisambiguationName = table.Column<string>(maxLength: 256, nullable: true),
                    ParsingStage = table.Column<int>(nullable: false),
                    StyleId = table.Column<int>(nullable: false),
                    ArtistId = table.Column<long>(nullable: false),
                    Mood = table.Column<string>(nullable: true),
                    Year = table.Column<int>(nullable: false),
                    LastModified = table.Column<DateTimeOffset>(nullable: false),
                    Cover_Sourcefile = table.Column<string>(nullable: true),
                    Cover_Filelength = table.Column<long>(nullable: true),
                    Cover_LastModified = table.Column<DateTimeOffset>(nullable: true),
                    Cover_MimeType = table.Column<string>(nullable: true),
                    Cover_Data = table.Column<byte[]>(nullable: true),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Works", x => x.Id);
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
                    AlphamericName = table.Column<string>(maxLength: 512, nullable: true),
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
                    PlaylistId = table.Column<long>(nullable: true)
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
                        onDelete: ReferentialAction.Restrict);
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
                    AlphamericTitle = table.Column<string>(maxLength: 256, nullable: true),
                    CompressedName = table.Column<string>(maxLength: 16, nullable: true),
                    CompositionName = table.Column<string>(maxLength: 256, nullable: false),
                    OriginalTitle = table.Column<string>(maxLength: 256, nullable: false),
                    MbidName = table.Column<string>(nullable: true),
                    IdTagName = table.Column<string>(maxLength: 256, nullable: true),
                    UserProvidedName = table.Column<string>(nullable: true),
                    UID = table.Column<Guid>(nullable: false),
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
                name: "IX_Artists_AlphamericName",
                schema: "music",
                table: "Artists",
                column: "AlphamericName",
                unique: true,
                filter: "[AlphamericName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ArtistWorkList_WorkId",
                schema: "music",
                table: "ArtistWorkList",
                column: "WorkId");

            migrationBuilder.CreateIndex(
                name: "IX_CompositionPerformances_PerformanceId",
                schema: "music",
                table: "CompositionPerformances",
                column: "PerformanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Compositions_ArtistId_AlphamericName",
                schema: "music",
                table: "Compositions",
                columns: new[] { "ArtistId", "AlphamericName" },
                unique: true,
                filter: "[AlphamericName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_PlaylistId",
                schema: "music",
                table: "Devices",
                column: "PlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_IdTags_MusicFileId",
                schema: "music",
                table: "IdTags",
                column: "MusicFileId");

            migrationBuilder.CreateIndex(
                name: "IX_IdTags_Name",
                schema: "music",
                table: "IdTags",
                column: "Name");

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
                name: "IX_PerformancePerformers_PerformerId",
                schema: "music",
                table: "PerformancePerformers",
                column: "PerformerId");

            migrationBuilder.CreateIndex(
                name: "IX_Performances_AlphamericPerformers",
                schema: "music",
                table: "Performances",
                column: "AlphamericPerformers");

            migrationBuilder.CreateIndex(
                name: "IX_Performers_AlphamericName_Type",
                schema: "music",
                table: "Performers",
                columns: new[] { "AlphamericName", "Type" },
                unique: true,
                filter: "[AlphamericName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistItems_PlaylistId",
                schema: "music",
                table: "PlaylistItems",
                column: "PlaylistId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Ragas_AlphamericName",
                schema: "music",
                table: "Ragas",
                column: "AlphamericName",
                unique: true,
                filter: "[AlphamericName] IS NOT NULL");

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
                name: "IX_Tracks_AlphamericTitle",
                schema: "music",
                table: "Tracks",
                column: "AlphamericTitle");

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_PerformanceId",
                schema: "music",
                table: "Tracks",
                column: "PerformanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_WorkId",
                schema: "music",
                table: "Tracks",
                column: "WorkId");

            migrationBuilder.CreateIndex(
                name: "IX_Works_AlphamericName",
                schema: "music",
                table: "Works",
                column: "AlphamericName");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArtistStyles",
                schema: "music");

            migrationBuilder.DropTable(
                name: "ArtistWorkList",
                schema: "music");

            migrationBuilder.DropTable(
                name: "CompositionPerformances",
                schema: "music");

            migrationBuilder.DropTable(
                name: "Devices",
                schema: "music");

            migrationBuilder.DropTable(
                name: "IdTags",
                schema: "music");

            migrationBuilder.DropTable(
                name: "PerformancePerformers",
                schema: "music");

            migrationBuilder.DropTable(
                name: "PlaylistItems",
                schema: "music");

            migrationBuilder.DropTable(
                name: "RagaPerformances",
                schema: "music");

            migrationBuilder.DropTable(
                name: "TaskItems",
                schema: "music");

            migrationBuilder.DropTable(
                name: "Compositions",
                schema: "music");

            migrationBuilder.DropTable(
                name: "MusicFiles",
                schema: "music");

            migrationBuilder.DropTable(
                name: "Performers",
                schema: "music");

            migrationBuilder.DropTable(
                name: "Playlists",
                schema: "music");

            migrationBuilder.DropTable(
                name: "Ragas",
                schema: "music");

            migrationBuilder.DropTable(
                name: "Artists",
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
        }
    }
}
