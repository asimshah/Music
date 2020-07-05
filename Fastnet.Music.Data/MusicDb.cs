using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Proxies;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Music.Data
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public class RetryStrategy : SqlServerRetryingExecutionStrategy
    {
        public RetryStrategy( ExecutionStrategyDependencies dependencies, int maxRetryCount) : base(dependencies, maxRetryCount)
        {
        }

        public int RetryNumber { get; set; } = 0;
        protected override void OnRetry()
        {
            RetryNumber++;
            base.OnRetry();
        }
        protected override bool ShouldRetryOn(Exception exception)
        {
            //Debug.WriteLine($"ShouldRetryOn() called with {exception.GetType().Name}, retry number is {RetryNumber}");
            return true;
        }
    }
    public class MusicDb : DbContext
    {
        private string connectionString;
        private readonly ILogger log;
        public DbSet<MusicFile> MusicFiles { get; set; }
        public DbSet<IdTag> IdTags { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Work> Works { get; set; }
        public DbSet<ArtistWork> ArtistWorkList { get; set; }
        public DbSet<Composition> Compositions { get; set; }
        public DbSet<CompositionPerformance> CompositionPerformances { get; set; }
        public DbSet<Raga> Ragas { get; set; }
        public DbSet<RagaPerformance> RagaPerformances { get; set; }
        public DbSet<Performance> Performances { get; set; }
        public DbSet<Performer> Performers { get; set; }
        public DbSet<PerformancePerformer> PerformancePerformers { get; set; }
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
            try
            {
                if (this.ChangeTracker.AutoDetectChangesEnabled == false)
                {
                    this.ChangeTracker.DetectChanges();
                }
                return base.SaveChanges();
            }
            catch (Exception xe)
            {
                log.Error(xe);
                throw;
            }
        }
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (this.ChangeTracker.AutoDetectChangesEnabled == false)
                {
                    this.ChangeTracker.DetectChanges();
                }
                return base.SaveChangesAsync(cancellationToken);
            }
            catch (Exception xe)
            {
                log.Error(xe);
                throw;
            }
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if(!optionsBuilder.IsConfigured)
            {
                //var lf = LoggerFactory.Create(builder =>
                //{
                //    builder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Information)
                //    .AddDebug()
                //    .Services.AddSingleton<ILoggerProvider>((sp) =>
                //    {
                //        return new RollingFileLoggerProvider((c, l) => l > LogLevel.Debug, false);
                //    });

                //});
                
                optionsBuilder.UseSqlServer(connectionString, options =>
                {
                    options.EnableRetryOnFailure();
                    options.ExecutionStrategy(x => new RetryStrategy(x, 4));
                })
                    .EnableDetailedErrors()
                    .EnableSensitiveDataLogging()
                    //.UseLoggerFactory(lf)
                    .UseLazyLoadingProxies();
            }
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

            modelBuilder.Entity<Artist>()
                .HasIndex(e => e.AlphamericName)
                .IsUnique();

            modelBuilder.Entity<Work>()
                .HasIndex(e => e.AlphamericName);

            modelBuilder.Entity<ArtistWork>()
                .HasKey(k => new { k.ArtistId, k.WorkId });

            modelBuilder.Entity<ArtistWork>()
                .HasOne(aw => aw.Artist)
                .WithMany(x => x.ArtistWorkList)
                .HasForeignKey(x => x.ArtistId);

            modelBuilder.Entity<ArtistWork>()
                .HasOne(aw => aw.Work)
                .WithMany(x => x.ArtistWorkList)
                .HasForeignKey(x => x.WorkId);

            modelBuilder.Entity<Composition>()
                .HasIndex(e => new { e.ArtistId, e.AlphamericName })
                .IsUnique();

            modelBuilder.Entity<CompositionPerformance>()
                .HasKey(k => new { k.CompositionId, k.PerformanceId })
                ;

            modelBuilder.Entity<CompositionPerformance>()
                .HasOne(aw => aw.Composition)
                .WithMany(x => x.CompositionPerformances)
                .HasForeignKey(x => x.CompositionId)
                .OnDelete(DeleteBehavior.NoAction)
                ;


            modelBuilder.Entity<CompositionPerformance>()
                .HasOne(aw => aw.Performance)
                .WithMany(x => x.CompositionPerformances)
                .HasForeignKey(x => x.PerformanceId)
                .OnDelete(DeleteBehavior.NoAction)
                ;

            modelBuilder.Entity<CompositionPerformance>()
                .HasIndex(e => e.PerformanceId)
                .IsUnique();

            modelBuilder.Entity<Raga>()
                .HasIndex(e => e.AlphamericName)
                .IsUnique();

            modelBuilder.Entity<RagaPerformance>()
                .HasKey(k => new { k.ArtistId, k.RagaId, k.PerformanceId });

            modelBuilder.Entity<RagaPerformance>()
                .HasOne(rp => rp.Artist)
                .WithMany(x => x.RagaPerformances)
                .HasForeignKey(x => x.ArtistId);

            modelBuilder.Entity<Performance>()
                .HasIndex(e => e.AlphamericPerformers);

            modelBuilder.Entity<Performer>()
                //.HasIndex(e => new { e.Type, e.Name })
                .HasIndex(e => new {e.AlphamericName, e.Type})
                .IsUnique();

            modelBuilder.Entity<PerformancePerformer>()
                .HasKey(k => new { k.PerformanceId, k.PerformerId });

            modelBuilder.Entity<PerformancePerformer>()
                .HasOne(pp => pp.Performance)
                .WithMany(p => p.PerformancePerformers)
                .HasForeignKey(pp => pp.PerformanceId);

            modelBuilder.Entity<PerformancePerformer>()
                .HasOne(pp => pp.Performer)
                .WithMany(p => p.PerformancePerformers)
                .HasForeignKey(pp => pp.PerformerId);

            modelBuilder.Entity<ArtistStyle>()
                .HasKey(x => new { x.ArtistId, x.StyleId });

            modelBuilder.Entity<Track>()
                .HasIndex(e => e.AlphamericTitle);

            modelBuilder.Entity<Track>()
                .HasOne(x => x.Work)
                .WithMany(x => x.Tracks)
                .HasForeignKey(k => k.WorkId);

            modelBuilder.Entity<Device>()
               .HasOne(x => x.Playlist)
               .WithMany()
               .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PlaylistItem>()
                .HasOne(x => x.Playlist)
                .WithMany(x => x.Items)
                .HasForeignKey(k => k.PlaylistId);

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
        public void UpgradeContent(MusicOptions options)
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

            EnsureArtistReputation();
            EnsureArtistAlphamericNames();
            EnsurePerformanceMusicStyle();
            EnsurePerformersRefactored(options);
            EnsureArtistWorkRefactored();
            EnsureCompositionPerformanceRefactored();
            EnsurePerformerAlphamericNames();
            CleanUpOrphanedPlaylists();
            SaveChanges();
        }

        private void CleanUpOrphanedPlaylists()
        {
            var allDevicePlaylists = Playlists.Where(x => x.Type == PlaylistType.DeviceList);
            var validDevicePlaylists = Devices.Select(x => x.Playlist);
            var removableDevicePlaylists = allDevicePlaylists.Except(validDevicePlaylists);
            foreach(var pl in removableDevicePlaylists)
            {
                Playlists.Remove(pl);
                log.Information($"orphaned device playlist {((IIdentifier)pl).ToIdent()} removed");
            }
        }

        private void EnsureArtistAlphamericNames()
        {
            var artists = Artists.Where(x => string.IsNullOrWhiteSpace(x.AlphamericName));
            foreach (var artist in artists)
            {
                artist.AlphamericName = artist.Name.ToAlphaNumerics();
            }
        }
        private void EnsureArtistReputation()
        {
            var artists = Artists.Where(x => x.Reputation == Reputation.NotDefined);
            foreach (var artist in artists)
            {
                artist.Reputation = Reputation.Average;
            }
        }
        private void EnsurePerformerAlphamericNames()
        {
            var performers = Performers.Where(x => string.IsNullOrWhiteSpace(x.AlphamericName));
            foreach(var performer in performers)
            {
                performer.AlphamericName = performer.Name.ToAlphaNumerics().ToLower();
            }
        }

        private void EnsurePerformanceMusicStyle()
        {
            var wplist = Works.Where(x => x.Tracks.All(t => t.Performance != null))
                .SelectMany(x => x.Tracks)
                .Where(t => t.Performance.StyleId != t.Work.StyleId)
                .Select(x => new { Work = x.Work, Performance = x.Performance})
                .Distinct()
                ;

            foreach(var item in wplist)
            {
                item.Performance.StyleId = item.Work.StyleId;
                log.Information($"{item.Performance.ToIdent()} set to {item.Performance.StyleId}");
            }

        }

        private void EnsureCompositionPerformanceRefactored()
        {
            var performances = Performances.Where(p => p.CompositionId > 0);
            foreach (var performance in performances)
            {
                var composition = Compositions.Find(performance.CompositionId);
                var cp = this.AddPerformance(composition, performance);
                performance.CompositionId = 0;
                log.Information($"{cp.ToIdent()} added");
            }
        }

        private void EnsureArtistWorkRefactored()
        {
            var works = Works.AsEnumerable()
                .Where(w => w.ArtistId > 0); 
            foreach(var work in works)
            {
                var artist = Artists.Find(work.ArtistId);
                var aw = this.AddWork(artist, work);
                work.ArtistId = 0;
                log.Information($"{aw.ToIdent()} added");
            }
        }

        private void EnsurePerformersRefactored(MusicOptions musicOptions)
        {
            void AddPerformer(string performerName, PerformerType type, Performance performance)
            {
                var name = musicOptions.ReplaceAlias(performerName);
                var performer = Performers.Local
                    .Where(x => x.Type == type)
                    .ToArray()
                    .SingleOrDefault(x => x.Name.IsEqualIgnoreAccentsAndCase(name));
                if(performer == null)
                {
                    performer = new Performer
                    {
                        Name = name,
                        AlphamericName = name.ToAlphaNumerics().ToLower(),
                        Type = type
                    };
                    Performers.Add(performer);
                }
                var pp = new PerformancePerformer
                {
                    Performer = performer,
                    Performance = performance,
                };
                PerformancePerformers.Add(pp);
                //log.Information($"[A-{performance.Composition.Artist.Id}] {performance.Composition.Artist.Name}, [C-{performance.Composition.Id}] {performance.Composition.Name} [P-{performance.Id}] [Pf-{performer.Id}] {performer.Name} added");
                log.Information($"{performance.ToLogIdentity()} [Pf-{performer.Id}] {performer.Name} added");
            }
            IEnumerable<string> split(string text)
            {
                text ??= string.Empty;
                return text.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
            }
            Performers.Load();
            foreach (var performance in Performances)
            {
                if (performance.PerformancePerformers.Count() == 0)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    var conductors = split(performance.Conductors);
                    var orchestras = split(performance.Orchestras);
                    var others = split(performance.Performers);
                    foreach(var item in conductors)
                    {
                        AddPerformer(item, PerformerType.Conductor, performance);
                    }
                    foreach (var item in orchestras)
                    {
                        AddPerformer(item, PerformerType.Orchestra, performance);
                    }
                    foreach (var item in others)
                    {
                        AddPerformer(item, PerformerType.Other, performance);
                    }
                    performance.Conductors = null;
                    performance.Orchestras = null;
                    performance.Performers = null;
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }
        }


    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
