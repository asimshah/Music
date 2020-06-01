﻿// <auto-generated />
using System;
using Fastnet.Music.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Fastnet.Music.Data.Migrations
{
    [DbContext(typeof(MusicDb))]
    [Migration("20200517141803_AddRagaUniqueKey")]
    partial class AddRagaUniqueKey
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("music")
                .HasAnnotation("ProductVersion", "3.1.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Fastnet.Music.Data.Artist", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("AlphamericName")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("CompressedName")
                        .HasColumnType("nvarchar(16)")
                        .HasMaxLength(16);

                    b.Property<string>("IdTagName")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<DateTimeOffset>("LastModified")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("MbidName")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<string>("OriginalName")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<int>("ParsingStage")
                        .HasColumnType("int");

                    b.Property<int>("Reputation")
                        .HasColumnType("int")
                        .HasMaxLength(128);

                    b.Property<byte[]>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("rowversion");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.Property<Guid>("UID")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("UserProvidedName")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.HasKey("Id");

                    b.HasIndex("AlphamericName")
                        .IsUnique()
                        .HasFilter("[AlphamericName] IS NOT NULL");

                    b.ToTable("Artists");
                });

            modelBuilder.Entity("Fastnet.Music.Data.ArtistStyle", b =>
                {
                    b.Property<long>("ArtistId")
                        .HasColumnType("bigint");

                    b.Property<int>("StyleId")
                        .HasColumnType("int");

                    b.HasKey("ArtistId", "StyleId");

                    b.ToTable("ArtistStyles");
                });

            modelBuilder.Entity("Fastnet.Music.Data.ArtistWork", b =>
                {
                    b.Property<long>("ArtistId")
                        .HasColumnType("bigint");

                    b.Property<long>("WorkId")
                        .HasColumnType("bigint");

                    b.HasKey("ArtistId", "WorkId");

                    b.HasIndex("WorkId");

                    b.ToTable("ArtistWorkList");
                });

            modelBuilder.Entity("Fastnet.Music.Data.Composition", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("AlphamericName")
                        .HasColumnType("nvarchar(512)")
                        .HasMaxLength(512);

                    b.Property<long>("ArtistId")
                        .HasColumnType("bigint");

                    b.Property<string>("CompressedName")
                        .HasColumnType("nvarchar(16)")
                        .HasMaxLength(16);

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(512)")
                        .HasMaxLength(512);

                    b.HasKey("Id");

                    b.HasIndex("ArtistId", "AlphamericName")
                        .IsUnique()
                        .HasFilter("[AlphamericName] IS NOT NULL");

                    b.ToTable("Compositions");
                });

            modelBuilder.Entity("Fastnet.Music.Data.CompositionPerformance", b =>
                {
                    b.Property<long>("CompositionId")
                        .HasColumnType("bigint");

                    b.Property<long>("PerformanceId")
                        .HasColumnType("bigint");

                    b.HasKey("CompositionId", "PerformanceId");

                    b.HasIndex("PerformanceId")
                        .IsUnique();

                    b.ToTable("CompositionPerformances");
                });

            modelBuilder.Entity("Fastnet.Music.Data.Device", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<bool>("CanReposition")
                        .HasColumnType("bit");

                    b.Property<string>("DisplayName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("HostMachine")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsDefaultOnHost")
                        .HasColumnType("bit");

                    b.Property<bool>("IsDisabled")
                        .HasColumnType("bit");

                    b.Property<string>("KeyName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("LastSeenDateTime")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("MACAddress")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("MaxSampleRate")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PlayerUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("PlaylistId")
                        .HasColumnType("bigint");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.Property<float>("Volume")
                        .HasColumnType("real");

                    b.HasKey("Id");

                    b.HasIndex("PlaylistId");

                    b.ToTable("Devices");
                });

            modelBuilder.Entity("Fastnet.Music.Data.IdTag", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<long>("MusicFileId")
                        .HasColumnType("bigint");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.Property<long>("PictureChecksum")
                        .HasColumnType("bigint");

                    b.Property<byte[]>("PictureData")
                        .HasColumnType("varbinary(max)");

                    b.Property<string>("PictureMimeType")
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.Property<string>("Value")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("MusicFileId");

                    b.HasIndex("Name");

                    b.ToTable("IdTags");
                });

            modelBuilder.Entity("Fastnet.Music.Data.MusicFile", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<double?>("AverageBitRate")
                        .HasColumnType("float");

                    b.Property<int?>("BitsPerSample")
                        .HasColumnType("int");

                    b.Property<string>("DiskRoot")
                        .HasColumnType("nvarchar(256)")
                        .HasMaxLength(256);

                    b.Property<double?>("Duration")
                        .HasColumnType("float");

                    b.Property<int>("Encoding")
                        .HasColumnType("int");

                    b.Property<string>("File")
                        .HasColumnType("nvarchar(2048)")
                        .HasMaxLength(2048);

                    b.Property<DateTimeOffset>("FileLastWriteTimeUtc")
                        .HasColumnType("datetimeoffset");

                    b.Property<long>("FileLength")
                        .HasColumnType("bigint");

                    b.Property<bool>("IsFaulty")
                        .HasColumnType("bit");

                    b.Property<bool>("IsGenerated")
                        .HasColumnType("bit");

                    b.Property<bool>("IsMultiPart")
                        .HasColumnType("bit");

                    b.Property<DateTimeOffset>("LastCataloguedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset>("LastPlayedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<int?>("MaximumBitRate")
                        .HasColumnType("int");

                    b.Property<int?>("MinimumBitRate")
                        .HasColumnType("int");

                    b.Property<int>("Mode")
                        .HasColumnType("int");

                    b.Property<string>("Mood")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Musician")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<int>("MusicianType")
                        .HasColumnType("int");

                    b.Property<string>("OpusName")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<string>("OpusPath")
                        .HasColumnType("nvarchar(512)")
                        .HasMaxLength(512);

                    b.Property<int>("OpusType")
                        .HasColumnType("int");

                    b.Property<int>("ParsingStage")
                        .HasColumnType("int");

                    b.Property<string>("PartName")
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.Property<int>("PartNumber")
                        .HasColumnType("int");

                    b.Property<int?>("SampleRate")
                        .HasColumnType("int");

                    b.Property<int>("Style")
                        .HasColumnType("int");

                    b.Property<string>("StylePath")
                        .HasColumnType("nvarchar(256)")
                        .HasMaxLength(256);

                    b.Property<byte[]>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("rowversion");

                    b.Property<long?>("TrackId")
                        .HasColumnType("bigint");

                    b.Property<string>("Uid")
                        .HasColumnType("nvarchar(36)")
                        .HasMaxLength(36);

                    b.HasKey("Id");

                    b.HasIndex("DiskRoot");

                    b.HasIndex("File")
                        .IsUnique()
                        .HasFilter("[File] IS NOT NULL");

                    b.HasIndex("OpusPath");

                    b.HasIndex("Style");

                    b.HasIndex("StylePath");

                    b.HasIndex("TrackId");

                    b.HasIndex("Uid")
                        .IsUnique()
                        .HasFilter("[Uid] IS NOT NULL");

                    b.ToTable("MusicFiles");
                });

            modelBuilder.Entity("Fastnet.Music.Data.Performance", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("AlphamericPerformers")
                        .HasColumnType("nvarchar(2048)")
                        .HasMaxLength(2048);

                    b.Property<long>("CompositionId")
                        .HasColumnType("bigint");

                    b.Property<string>("CompressedName")
                        .HasColumnType("nvarchar(16)")
                        .HasMaxLength(16);

                    b.Property<string>("Conductors")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Orchestras")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Performers")
                        .HasColumnType("nvarchar(2048)")
                        .HasMaxLength(2048);

                    b.Property<int>("StyleId")
                        .HasColumnType("int");

                    b.Property<int>("Year")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("AlphamericPerformers");

                    b.ToTable("Performances");
                });

            modelBuilder.Entity("Fastnet.Music.Data.PerformancePerformer", b =>
                {
                    b.Property<long>("PerformanceId")
                        .HasColumnType("bigint");

                    b.Property<long>("PerformerId")
                        .HasColumnType("bigint");

                    b.Property<bool>("Selected")
                        .HasColumnType("bit");

                    b.HasKey("PerformanceId", "PerformerId");

                    b.HasIndex("PerformerId");

                    b.ToTable("PerformancePerformers");
                });

            modelBuilder.Entity("Fastnet.Music.Data.Performer", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("AlphamericName")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("AlphamericName", "Type")
                        .IsUnique()
                        .HasFilter("[AlphamericName] IS NOT NULL");

                    b.ToTable("Performers");
                });

            modelBuilder.Entity("Fastnet.Music.Data.Playlist", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ModificationUid")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Playlists");
                });

            modelBuilder.Entity("Fastnet.Music.Data.PlaylistItem", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<long>("ItemId")
                        .HasColumnType("bigint");

                    b.Property<long>("MusicFileId")
                        .HasColumnType("bigint");

                    b.Property<long>("PlaylistId")
                        .HasColumnType("bigint");

                    b.Property<int>("Sequence")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("PlaylistId");

                    b.ToTable("PlaylistItems");
                });

            modelBuilder.Entity("Fastnet.Music.Data.Raga", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("AlphamericName")
                        .HasColumnType("nvarchar(512)")
                        .HasMaxLength(512);

                    b.Property<string>("CompressedName")
                        .HasColumnType("nvarchar(16)")
                        .HasMaxLength(16);

                    b.Property<string>("DisplayName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(512)")
                        .HasMaxLength(512);

                    b.HasKey("Id");

                    b.HasIndex("AlphamericName")
                        .IsUnique()
                        .HasFilter("[AlphamericName] IS NOT NULL");

                    b.ToTable("Ragas");
                });

            modelBuilder.Entity("Fastnet.Music.Data.RagaPerformance", b =>
                {
                    b.Property<long>("ArtistId")
                        .HasColumnType("bigint");

                    b.Property<long>("RagaId")
                        .HasColumnType("bigint");

                    b.Property<long>("PerformanceId")
                        .HasColumnType("bigint");

                    b.HasKey("ArtistId", "RagaId", "PerformanceId");

                    b.HasIndex("PerformanceId");

                    b.HasIndex("RagaId");

                    b.ToTable("RagaPerformances");
                });

            modelBuilder.Entity("Fastnet.Music.Data.TaskItem", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset>("FinishedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<bool>("ForSingles")
                        .HasColumnType("bit");

                    b.Property<bool>("Force")
                        .HasColumnType("bit");

                    b.Property<int>("MusicStyle")
                        .HasColumnType("int");

                    b.Property<int>("RetryCount")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("ScheduledAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<string>("TaskString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<byte[]>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("rowversion");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ScheduledAt");

                    b.HasIndex("Status");

                    b.ToTable("TaskItems");
                });

            modelBuilder.Entity("Fastnet.Music.Data.Track", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("AlphamericTitle")
                        .HasColumnType("nvarchar(256)")
                        .HasMaxLength(256);

                    b.Property<string>("CompositionName")
                        .IsRequired()
                        .HasColumnType("nvarchar(256)")
                        .HasMaxLength(256);

                    b.Property<string>("CompressedName")
                        .HasColumnType("nvarchar(16)")
                        .HasMaxLength(16);

                    b.Property<string>("IdTagName")
                        .HasColumnType("nvarchar(256)")
                        .HasMaxLength(256);

                    b.Property<DateTimeOffset>("LastModified")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("MbidName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("MovementNumber")
                        .HasColumnType("int");

                    b.Property<int>("Number")
                        .HasColumnType("int");

                    b.Property<int>("NumberParsingStage")
                        .HasColumnType("int");

                    b.Property<string>("OriginalTitle")
                        .IsRequired()
                        .HasColumnType("nvarchar(256)")
                        .HasMaxLength(256);

                    b.Property<int>("ParsingStage")
                        .HasColumnType("int");

                    b.Property<long?>("PerformanceId")
                        .HasColumnType("bigint");

                    b.Property<byte[]>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("rowversion");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(256)")
                        .HasMaxLength(256);

                    b.Property<Guid>("UID")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("UserProvidedName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("WorkId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("AlphamericTitle");

                    b.HasIndex("PerformanceId");

                    b.HasIndex("WorkId");

                    b.ToTable("Tracks");
                });

            modelBuilder.Entity("Fastnet.Music.Data.Work", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("AlphamericName")
                        .HasColumnType("nvarchar(256)")
                        .HasMaxLength(256);

                    b.Property<long>("ArtistId")
                        .HasColumnType("bigint");

                    b.Property<string>("CompressedName")
                        .HasColumnType("nvarchar(16)")
                        .HasMaxLength(16);

                    b.Property<string>("DisambiguationName")
                        .HasColumnType("nvarchar(256)")
                        .HasMaxLength(256);

                    b.Property<string>("IdTagName")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<bool>("IsMultiPart")
                        .HasColumnType("bit");

                    b.Property<DateTimeOffset>("LastModified")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("MbidName")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<string>("Mood")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(256)")
                        .HasMaxLength(256);

                    b.Property<string>("OriginalName")
                        .IsRequired()
                        .HasColumnType("nvarchar(256)")
                        .HasMaxLength(256);

                    b.Property<int>("ParsingStage")
                        .HasColumnType("int");

                    b.Property<string>("PartName")
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.Property<int>("PartNumber")
                        .HasColumnType("int");

                    b.Property<int>("StyleId")
                        .HasColumnType("int");

                    b.Property<byte[]>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("rowversion");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.Property<Guid>("UID")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("UserProvidedName")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<int>("Year")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("AlphamericName");

                    b.ToTable("Works");
                });

            modelBuilder.Entity("Fastnet.Music.Data.Artist", b =>
                {
                    b.OwnsOne("Fastnet.Music.Data.Image", "Portrait", b1 =>
                        {
                            b1.Property<long>("ArtistId")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("bigint")
                                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                            b1.Property<byte[]>("Data")
                                .HasColumnType("varbinary(max)");

                            b1.Property<long>("Filelength")
                                .HasColumnType("bigint");

                            b1.Property<DateTimeOffset>("LastModified")
                                .HasColumnType("datetimeoffset");

                            b1.Property<string>("MimeType")
                                .HasColumnType("nvarchar(max)");

                            b1.Property<string>("Sourcefile")
                                .HasColumnType("nvarchar(max)");

                            b1.HasKey("ArtistId");

                            b1.ToTable("Artists");

                            b1.WithOwner()
                                .HasForeignKey("ArtistId");
                        });
                });

            modelBuilder.Entity("Fastnet.Music.Data.ArtistStyle", b =>
                {
                    b.HasOne("Fastnet.Music.Data.Artist", "Artist")
                        .WithMany("ArtistStyles")
                        .HasForeignKey("ArtistId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Fastnet.Music.Data.ArtistWork", b =>
                {
                    b.HasOne("Fastnet.Music.Data.Artist", "Artist")
                        .WithMany("ArtistWorkList")
                        .HasForeignKey("ArtistId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Fastnet.Music.Data.Work", "Work")
                        .WithMany("ArtistWorkList")
                        .HasForeignKey("WorkId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Fastnet.Music.Data.Composition", b =>
                {
                    b.HasOne("Fastnet.Music.Data.Artist", "Artist")
                        .WithMany("Compositions")
                        .HasForeignKey("ArtistId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Fastnet.Music.Data.CompositionPerformance", b =>
                {
                    b.HasOne("Fastnet.Music.Data.Composition", "Composition")
                        .WithMany("CompositionPerformances")
                        .HasForeignKey("CompositionId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("Fastnet.Music.Data.Performance", "Performance")
                        .WithMany("CompositionPerformances")
                        .HasForeignKey("PerformanceId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();
                });

            modelBuilder.Entity("Fastnet.Music.Data.Device", b =>
                {
                    b.HasOne("Fastnet.Music.Data.Playlist", "Playlist")
                        .WithMany()
                        .HasForeignKey("PlaylistId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Fastnet.Music.Data.IdTag", b =>
                {
                    b.HasOne("Fastnet.Music.Data.MusicFile", "MusicFile")
                        .WithMany("IdTags")
                        .HasForeignKey("MusicFileId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Fastnet.Music.Data.MusicFile", b =>
                {
                    b.HasOne("Fastnet.Music.Data.Track", "Track")
                        .WithMany("MusicFiles")
                        .HasForeignKey("TrackId");
                });

            modelBuilder.Entity("Fastnet.Music.Data.PerformancePerformer", b =>
                {
                    b.HasOne("Fastnet.Music.Data.Performance", "Performance")
                        .WithMany("PerformancePerformers")
                        .HasForeignKey("PerformanceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Fastnet.Music.Data.Performer", "Performer")
                        .WithMany("PerformancePerformers")
                        .HasForeignKey("PerformerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Fastnet.Music.Data.PlaylistItem", b =>
                {
                    b.HasOne("Fastnet.Music.Data.Playlist", "Playlist")
                        .WithMany("Items")
                        .HasForeignKey("PlaylistId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Fastnet.Music.Data.RagaPerformance", b =>
                {
                    b.HasOne("Fastnet.Music.Data.Artist", "Artist")
                        .WithMany("RagaPerformances")
                        .HasForeignKey("ArtistId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Fastnet.Music.Data.Performance", "Performance")
                        .WithMany("RagaPerformances")
                        .HasForeignKey("PerformanceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Fastnet.Music.Data.Raga", "Raga")
                        .WithMany()
                        .HasForeignKey("RagaId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Fastnet.Music.Data.Track", b =>
                {
                    b.HasOne("Fastnet.Music.Data.Performance", "Performance")
                        .WithMany("Movements")
                        .HasForeignKey("PerformanceId");

                    b.HasOne("Fastnet.Music.Data.Work", "Work")
                        .WithMany("Tracks")
                        .HasForeignKey("WorkId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Fastnet.Music.Data.Work", b =>
                {
                    b.OwnsOne("Fastnet.Music.Data.Image", "Cover", b1 =>
                        {
                            b1.Property<long>("WorkId")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("bigint")
                                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                            b1.Property<byte[]>("Data")
                                .HasColumnType("varbinary(max)");

                            b1.Property<long>("Filelength")
                                .HasColumnType("bigint");

                            b1.Property<DateTimeOffset>("LastModified")
                                .HasColumnType("datetimeoffset");

                            b1.Property<string>("MimeType")
                                .HasColumnType("nvarchar(max)");

                            b1.Property<string>("Sourcefile")
                                .HasColumnType("nvarchar(max)");

                            b1.HasKey("WorkId");

                            b1.ToTable("Works");

                            b1.WithOwner()
                                .HasForeignKey("WorkId");
                        });
                });
#pragma warning restore 612, 618
        }
    }
}