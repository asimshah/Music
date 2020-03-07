using Fastnet.Core;
using Fastnet.Core.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Proxies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Music.Data
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public class MusicDb : DbContext
    {
#pragma warning disable CS0169 // The field 'MusicDb.config' is never used
        //private IConfiguration config;
#pragma warning restore CS0169 // The field 'MusicDb.config' is never used
        private string connectionString;
        private readonly ILogger log;
        public DbSet<MusicFile> MusicFiles { get; set; }
        public DbSet<IdTag> IdTags { get; set; }
        //public DbSet<Style> Styles { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Work> Works { get; set; }
        public DbSet<Composition> Compositions { get; set; }
        public DbSet<Performance> Performances { get; set; }
        public DbSet<Track> Tracks { get; set; }
        public DbSet<ArtistStyle> ArtistStyles { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<PlaylistItem> PlaylistItems { get; set; }
        public DbSet<TaskItem> TaskItems { get; set; }

        public MusicDb(string cs)
        {
            this.connectionString = cs;
            log = ApplicationLoggerFactory.CreateLogger<MusicDb>();
        }
        public MusicDb(DbContextOptions<MusicDb> options) : base(options)
        {

            log = ApplicationLoggerFactory.CreateLogger<MusicDb>();
        }
        public override int SaveChanges()
        {
            if (this.ChangeTracker.AutoDetectChangesEnabled == false)
            {
                this.ChangeTracker.DetectChanges();
            }
            return base.SaveChanges();
        }
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (this.ChangeTracker.AutoDetectChangesEnabled == false)
            {
                this.ChangeTracker.DetectChanges();
            }
            return base.SaveChangesAsync(cancellationToken);
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if(!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(connectionString, options => { options.EnableRetryOnFailure(); })
                    .EnableDetailedErrors()
                    .EnableSensitiveDataLogging()
                    .UseLazyLoadingProxies();
            }
            //var cs = config.GetConnectionString("TestMusicDb");
            //base.OnConfiguring(optionsBuilder);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("music");

            modelBuilder.Entity<MusicFile>()
                .HasIndex(e => e.Style);
            modelBuilder.Entity<MusicFile>()
                .HasIndex(e => e.File)
                .IsUnique();

            modelBuilder.Entity<MusicFile>()
                .HasIndex(e => e.DiskRoot);
            modelBuilder.Entity<MusicFile>()
                .HasIndex(e => e.StylePath);
            modelBuilder.Entity<MusicFile>()
                .HasIndex(e => e.OpusPath);

            modelBuilder.Entity<MusicFile>()
                .HasIndex(e => e.Uid)
                .IsUnique();

            modelBuilder.Entity<MusicFile>()
                .HasOne(x => x.Track)
                .WithMany(x => x.MusicFiles)
                .HasForeignKey(x => x.TrackId);

            modelBuilder.Entity<IdTag>()
                .HasIndex(e => e.Name);

            //modelBuilder.Entity<IdTag>()
            //    .HasIndex(e => new { e.MusicFileId, e.Name })
            //    .IsUnique();

            modelBuilder.Entity<Artist>()
                .HasIndex(e => e.Name)
                .IsUnique();

            modelBuilder.Entity<Work>()
                .HasIndex(e => e.AlphamericName);

            modelBuilder.Entity<Composition>()
                .HasIndex(e => new { e.ArtistId, e.AlphamericName })
                .IsUnique();

            modelBuilder.Entity<Performance>()
                .HasIndex(e => e.AlphamericPerformers);

            modelBuilder.Entity<ArtistStyle>()
                .HasKey(x => new { x.ArtistId, x.StyleId });

            modelBuilder.Entity<Track>()
                .HasIndex(e => e.AlphamericTitle);

            modelBuilder.Entity<Track>()
                .HasOne(x => x.Work)
                .WithMany(x => x.Tracks)
                .HasForeignKey(k => k.WorkId);

            modelBuilder.Entity<Device>()
               .HasOne(x => x.Playlist);

            modelBuilder.Entity<PlaylistItem>()
                .HasOne(x => x.Playlist)
                .WithMany(x => x.Items)
                .HasForeignKey(k => k.PlaylistId);

            //modelBuilder.Entity<TaskItem>()
            //    .HasIndex(x => x.TargetPath);
            modelBuilder.Entity<TaskItem>()
                .HasIndex(x => x.ScheduledAt);
            modelBuilder.Entity<TaskItem>()
                .HasIndex(x => x.Status);

            base.OnModelCreating(modelBuilder);
        }
        /// <summary>
        /// Called at the end of the database Initialiser and used to repair and/or upgrade the database
        /// Note this method must contain re-runnable code as it executes at every app startup
        /// </summary>
        public void UpgradeContent()
        {

            foreach (var track in Tracks.Where(t => t.AlphamericTitle == null))
            {
                log.Warning($"track {track.Title} [T-{track.Id}] has no alphameric text");
                track.AlphamericTitle = track.Title.ToAlphaNumerics().ToLower();
            }
            foreach (var work in Works.Where(w => w.AlphamericName == null))
            {
                log.Warning($"work {work.Name} [W-{work.Id}] has no alphameric text");
                work.AlphamericName = work.Name.ToAlphaNumerics().ToLower();
            }
            foreach (var performance in Performances.Where(w => w.AlphamericPerformers == null))
            {
                log.Warning($"performance {performance.Performers} [P-{performance.Id}] has no alphameric text");
                performance.AlphamericPerformers = performance.Performers.ToAlphaNumerics().ToLower();
            }
            foreach (var composition in Compositions.Where(w => w.AlphamericName == null))
            {
                log.Warning($"composition {composition.Name} [C-{composition.Id}] has no alphameric text");
                composition.AlphamericName = composition.Name.ToAlphaNumerics().ToLower();
            }


            var toBeRemoved = TaskItems.Where(x => x.Type != TaskType.ResampleWork).ToList();
            toBeRemoved.AddRange(TaskItems.Where(x => x.Status == Core.TaskStatus.Finished || x.Status == Core.TaskStatus.Failed));
            TaskItems.RemoveRange(toBeRemoved);
            log.Information($"{toBeRemoved.Count()} task items removed");
            TaskItems.ToList().ForEach(x => x.Status = Core.TaskStatus.Pending);
            SaveChanges();
//#if DEBUG
//            var allItems = TaskItems.ToArray();
//            TaskItems.RemoveRange(allItems);
//            log.Warning($"{allItems.Count()} task items removed");
//#else
//            var staleTaskItemDate = DateTimeOffset.Now - TimeSpan.FromDays(5);
//            foreach (var item in TaskItems.Where(t => t.CreatedAt < staleTaskItemDate).ToArray())
//            {
//                switch(item.Status)
//                {
//                    case Core.TaskStatus.Finished:
//                        break;
//                    case Core.TaskStatus.Failed:
//                        log.Warning($"Task {item.Type} created on {item.CreatedAt.ToDefaultWithTime()} for {item.TaskString} failed - removed");
//                        break;
//                    default:
//                        log.Warning($"Task {item.Type} created on {item.CreatedAt.ToDefaultWithTime()} for {item.TaskString}, status {item.Status} - removed");
//                        break;
//                }
//                TaskItems.Remove(item);
//            }
//#endif
//            SaveChanges();
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
