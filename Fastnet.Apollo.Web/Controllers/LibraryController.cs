using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Core.Web.Controllers;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Metatools;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Fastnet.Music.Resampler;

namespace Fastnet.Apollo.Web.Controllers
{
    [Route("lib")]
    [ApiController]
    public class LibraryController : BaseController
    {
        private static readonly Regex ipadRegex = new Regex(@"(android|ipad|playbook|silk|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
        private MusicOptions musicOptions;
        private readonly string contentRootPath;
        //private readonly SchedulerService ss;
        private readonly MusicServerOptions musicServerOptions;
        private readonly MusicDb musicDb;
        private readonly TaskRunner taskRunner;
        private readonly TaskPublisher taskPublisher;
        public LibraryController(/*SchedulerService schedulerService ,*/ IWebHostEnvironment env,
            IOptions<MusicServerOptions> mso, /*IServiceProvider sp,*/
            TaskPublisher tp,  TaskRunner tr,
            IOptionsMonitor<MusicOptions> mo, ILogger<LibraryController> logger, MusicDb mdb) : base(logger, env)
        {
            this.musicOptions = mo.CurrentValue;
            //this.ss = schedulerService;// hs as SchedulerService;
            mo.OnChangeWithDelay((opt) =>
            {
                this.musicOptions = opt;
            });
            this.contentRootPath = env.ContentRootPath;
            this.log = logger;
            this.musicServerOptions = mso.Value;
            this.musicDb = mdb;
            this.taskPublisher = tp;
            this.taskRunner = tr;
        }
        [HttpGet("parameters/get/{key?}")]
        public IActionResult GetParameters(string key = null)
        {
            Debug.Assert(key != "undefined");
            if (string.IsNullOrWhiteSpace(key))
            {
                key = Guid.NewGuid().ToString().ToLower();
                log.Information($"new browser key {key} allocated");
            }

            var styles = this.musicOptions.Styles.Select(s => new StyleDTO { Id = s.Style, Enabled = s.Enabled, DisplayName = s.Style.ToDescription() }).ToArray();
            var dto = new ParametersDTO
            {
                Version = GetPackageVersion(),
                BrowserKey = key,
                AppName = this.environment.IsDevelopment() ? "Apollo Dev" : "Apollo",
                IsMobile = this.Request.IsMobileBrowser(),
                IsIpad = this.Request.IsIpad(),// this.IsMobile == false && ipadRegex.IsMatch(Request.UserAgent()),
                Browser = this.Request.GetBrowser().ToString(),
                ClientIPAddress = this.Request.HttpContext.GetRemoteIPAddress(),
                CompactLayoutWidth = this.musicServerOptions.CompactLayoutWidth,
                Styles = styles
            };
            return SuccessResult(dto);
        }
        [HttpGet("music/options")]
        public IActionResult GetMusicOptions()
        {
            return SuccessResult(musicOptions);
        }
        //[HttpGet("style/information")]
        //public IActionResult GetStyleInformation()
        //{
        //    return SuccessResult(options.Styles);
        //}
        [HttpGet("search/{style}/{searchText}")]
        public async Task<IActionResult> Search(MusicStyles style, string searchText)
        {
            await Task.Delay(0);
            log.Debug($"Search for {searchText} ...");
            var cs = CatalogueSearcher.GetSearcher(this.musicOptions, style, this.musicDb, this.log);
            using (new TimedAction((t) => log.Debug($"    ... completed in {t.ToString("c")}")))
            {
                var (prefixMode, results) = cs.Search(searchText);
                return SuccessResult(new { prefixMode, results });
            }
        }
        [HttpGet("get/{style}/allartists")]
        public IActionResult GetAllArtists(MusicStyles style)
        {
            var artists = musicDb.ArtistStyles.Where(x => x.StyleId == style && x.Artist.Type != ArtistType.Various)
                .Select(x => x.Artist.Id);
                return SuccessResult(artists);
        }
        [HttpGet("get/artist/{id}")]
        public async Task<IActionResult> GetArtistInfo(long id)
        {
            var artist = await musicDb.Artists
                .SingleOrDefaultAsync(x => x.Id == id);
            if (artist == null)
            {
                return ErrorResult($"Artist with id {id} not found");
            }
            else
            {
                return SuccessResult(artist.ToDTO());
            }
        }
        [HttpGet("get/composition/{id}")]
        public async Task<IActionResult> GetComposition(long id)
        {
            var composition = await musicDb.Compositions
                .SingleOrDefaultAsync(x => x.Id == id);
            if (composition == null)
            {
                return ErrorResult($"composition with id {id} not found");
            }
            else
            {
                return SuccessResult(composition.ToDTO());
            }
        }
        [HttpGet("get/work/{id}")]
        public async Task<IActionResult> GetWork(long id)
        {
            var work = await musicDb.Works
                .SingleOrDefaultAsync(x => x.Id == id);
            if (work == null)
            {
                return ErrorResult($"work with id {id} not found");
            }
            else
            {
                return SuccessResult(work.ToDTO());
            }
        }
        [HttpGet("get/track/{id}")]
        public async Task<IActionResult> GetTrack(long id)
        {
            var track = await musicDb.Tracks
                .SingleOrDefaultAsync(x => x.Id == id);
            if (track == null)
            {
                return ErrorResult($"track with id {id} not found");
            }
            else
            {
                return SuccessResult(track.ToDTO());
            }
        }
        [HttpGet("get/performance/{id}/{full?}")]
        public async Task<IActionResult> GetPerformance(long id, bool full = false)
        {
            var performance = await musicDb.Performances
                .SingleOrDefaultAsync(x => x.Id == id);
            if (performance == null)
            {
                return ErrorResult($"performance with id {id} not found");
            }
            else
            {
                return SuccessResult(performance.ToDTO(full));
            }
        }
        [HttpGet("edit/performance/{id}/")]
        public async Task<IActionResult> EditPerformance(long id)
        {
            var performance = await musicDb.Performances.SingleOrDefaultAsync(x => x.Id == id);
            if (performance == null)
            {
                return ErrorResult($"performance with id {id} not found");
            }
            var result = await performance.ToWesternClassicalAlbumTEO(musicOptions);
            return SuccessResult(result);
        }

        [HttpGet("edit/work/{id}")]
        public async Task<IActionResult> EditAlbum(long id)
        {
            var work = await musicDb.Works.SingleOrDefaultAsync(x => x.Id == id);
            if (work == null)
            {
                return ErrorResult($"work with id {id} not found");
            }
            var result = await work.ToPopularAlbumTEO(musicOptions);
            return SuccessResult(result);
        }
        [HttpPost("update/work/{style}")]
        public async Task<IActionResult> UpdateWork(MusicStyles style)
        {
            ITEOBase teo = null;
            switch(style)
            {
                case MusicStyles.Popular:
                    teo = await this.Request.FromBody<PopularAlbumTEO>();
                    break;
                case MusicStyles.WesternClassical:
                    teo = await this.Request.FromBody<WesternClassicalAlbumTEO>();
                    break;
            }
            var id = teo.Id;
            var work = await musicDb.Works.FindAsync(id);
            teo.AfterDeserialisation(work);
            teo.SaveChanges(musicDb, work);
            work.LastModified = DateTimeOffset.Now;
            await musicDb.SaveChangesAsync();
            return SuccessResult();
        }
        [HttpGet("get/composition/allperformances/{id}/{full?}")]
        public async Task<IActionResult> GetAllPerformances(long id, bool full = false)
        {
            musicDb.ChangeTracker.AutoDetectChangesEnabled = false;
            var performances = await musicDb.Performances
                .Where(x => x.CompositionId == id).ToArrayAsync();
            if (performances.Count() > 0)
            {
                var result = performances.Select(x => x.ToDTO(full))
                    .OrderByDescending(x => x.MovementCount)
                    .ThenBy(x => x.Performers);
                return SuccessResult(result);
            }
            else
            {
                var composition = await musicDb.Compositions.FindAsync(id);
                if(composition != null)
                {
                    log.Error($"No performances found for {composition.Artist.Name} composition {composition.Name}");
                }
                else
                {
                    log.Error($"Composition {id} not found");
                }
                return ErrorResult("Composition and/or performances not found");
            }
        }
        [HttpGet("get/artist/allcompositions/{id}/{full?}")]
        public IActionResult GetAllCompositions(long id, bool full = false)
        {
            musicDb.ChangeTracker.AutoDetectChangesEnabled = false;
            var compositions = musicDb.Compositions
                .Where(w => w.ArtistId == id)
                .ToArray()
                .OrderBy(x => x.Name, new NaturalStringComparer());
            var list = compositions.Select(x => x.ToDTO(full));
            return SuccessResult(list);
        }
        [HttpGet("get/artist/allworks/{id}/{full?}")]
        public async Task<IActionResult> GetAllWorks(long id, bool full = false)
        {
            //using (new TimedAction((t) => log.Trace($"Get All Compositions for Artist id {id} completed in {t.ToString("c")}")))
            //{
            await Task.Delay(0);
            musicDb.ChangeTracker.AutoDetectChangesEnabled = false;
            var works = musicDb.Works.AsEnumerable()
                //.Where(w => w.ArtistId == id)
                .Where(w => w.Artists.Select(x => x.Id).Contains(id))
                .ToArray()
                .OrderBy(x => x.Name, new NaturalStringComparer());
            var list = works.Select(x => x.ToDTO(full));
            return SuccessResult(list);
            //}
        }
        [HttpGet("get/performance/allmovements/{id}")]
        public async Task<IActionResult> GetPerformanceAllMovements(long id)
        {
            var performance = await musicDb.Performances
                .SingleAsync(x => x.Id == id)
                ;
            var movements = performance.Movements.OrderBy(x => x.Number);
            //return SuccessResult(movements.Select(x => x.ToDTO()));
            return SuccessResult(movements.ToDetails());
        }
        [HttpGet("get/performance/details/{id}")]
        public async Task<IActionResult> GetPerformanceDetails(long id)
        {
            var performance = await musicDb.Performances
                .SingleAsync(x => x.Id == id)
                ;
            //var movements = performance.Movements.OrderBy(x => x.Number);
            return SuccessResult(performance.ToDetails());
        }
        [HttpGet("get/work/details/{id}")]
        public async Task<IActionResult> GetWorkDetails(long id)
        {
            var work = await musicDb.Works
                .SingleAsync(x => x.Id == id)
                ;
            //var tracks = work.Tracks.OrderBy(x => x.Number);
            return SuccessResult(work.ToDetails());
        }
        [HttpGet("get/work/alltracks/{id}")]
        public async Task<IActionResult> GetWorkAllTracks(long id)
        {
            var work = await musicDb.Works
                .SingleAsync(x => x.Id == id)
                ;
            var q = work.Tracks;
            IOrderedEnumerable<Track> tracks = null;
            if (work.Type == OpusType.Singles || work.Type == OpusType.Collection)
            {
                tracks = work.Tracks.OrderBy(x => x.Title);

            }
            else
            {
                tracks = work.Tracks.OrderBy(x => x.Number);
            }
            return SuccessResult(tracks.Select(x => x.ToDTO()));
        }
        [HttpGet("get/artist/imageart/{id}")]
        public async Task<IActionResult> GetArtistImageArt(long id)
        {
            //using (new TimedAction((t) => log.Trace($"Get Imageart for artist id {id} completed in {t.ToString("c")}")))
            //{
            var artist = await musicDb.Artists.FindAsync(id);
            if (artist == null)
            {
                return ErrorResult($"Artist with id {id} not found");
            }
            else if(artist.Portrait != null)
            {
                return GetImageResult(artist.Portrait);
                //var image = artist.Portrait.Data;// artist.ImageData;
                //var ms = new MemoryStream(image);
                //return CacheableResult(new FileStreamResult(ms, artist.Portrait.MimeType), artist.Portrait.LastModified);
            }
            else
            {
                ErrorResult($"No image art found for artist {id}");
            }
            return ErrorResult($"Artist with id {id} not found");
            //}
        }
        [HttpGet("get/work/coverart/{id}")]
        public async Task<IActionResult> GetWorkCoverArt(long id)
        {
            var work = await musicDb.Works.FindAsync(id);
            if (work == null)
            {
                return ErrorResult($"Work with id {id} not found");
            }
            else if(work.Cover != null)
            {
                return GetImageResult(work.Cover);
                //var image = work.CoverData;
                //if (image != null && image.Length > 0)
                //{
                //    var ms = new MemoryStream(image);
                //    return CacheableResult(new FileStreamResult(ms, work.CoverMimeType), work.CoverDateTime);
                //}
                //else
                //{

                //}
            }
            else
            {
                ErrorResult($"No cover art found for work {id}");
            }
            return ErrorResult($"Work with id {id} not found");
            //}
        }
        //[HttpPost("update/performance")]
        //public async Task<IActionResult> UpdatePerformance()
        //{
        //    var dto = this.Request.FromBody<PerformanceDTO>();
        //    //var text = dto.ToJson();
        //    //log.Information(text);

        //    try
        //    {
        //        var performance = await musicDb.Performances.FindAsync(dto.Id);
        //        Debug.Assert(performance.Movements.Count() == dto.Movements.Count(), "UpdatePerformance() does not supprt changing the number of movements");
        //        performance.Performers = dto.Performers;
        //        foreach (var item in dto.Movements)
        //        {
        //            var movement = performance.Movements.Single(x => x.Id == item.Id);
        //            movement.Number = item.Number;
        //            movement.Title = item.Title;
        //        }
        //        if (musicDb.ChangeTracker.HasChanges())
        //        {
        //            log.Information($"Composer {performance.Composition.Artist.Name}, Composition {performance.Composition.Name}, performance {performance.Performers} metadata changed");
        //        }
        //        await musicDb.SaveChangesAsync();
        //        return SuccessResult();
        //    }
        //    catch (Exception xe)
        //    {
        //        log.Error(xe);
        //        return ExceptionResult(xe);
        //    }
        //}
        //
        [HttpGet("reset/artist/{id}")]
        public async Task<IActionResult> ResetArtist(long id)
        {
            var artist = await musicDb.Artists
                .SingleOrDefaultAsync(x => x.Id == id);
            foreach (var work in artist.Works)
            {
                await ResetWork(work.Id);
            }
            log.Information($"Artist [A-{artist.Id}]{artist.Name} reset");
            return SuccessResult();
        }
        [HttpGet("reset/work/{id}")]
        public async Task<IActionResult> ResetWork(long id)
        {
            var work = await musicDb.Works
                .SingleOrDefaultAsync(x => x.Id == id);
            if (work != null)
            {
                var tracks = work.Tracks;// artist.Works.SelectMany(x => x.Tracks);
                var musicFiles = tracks.SelectMany(x => x.MusicFiles)
                    .Where(x => x.IsGenerated == false);
                //var paths = work.Type == OpusType.Collection ?
                //    musicFiles.Select(x => Path.Combine(x.DiskRoot, x.StylePath, "Collections", x.OpusPath)).Distinct(StringComparer.CurrentCultureIgnoreCase)
                //    : musicFiles.Select(x => Path.Combine(x.DiskRoot, x.StylePath, x.OpusPath)).Distinct(StringComparer.CurrentCultureIgnoreCase);
                var paths = musicFiles.Select(x => x.GetRootPath()).Distinct(StringComparer.CurrentCultureIgnoreCase);
                foreach (var path in paths)
                {
                    await taskPublisher.AddTask(work.StyleId, TaskType.DiskPath, path, true);
                    log.Information($"[A-{work.GetArtistIds()}] {work.GetArtistNames()} [W-{work.Id}] {work.Name} {path} forcibly recatalogued");
                }
            }
            return SuccessResult();
        }
        [HttpGet("reset/performance/{id}")]
        public async Task<IActionResult> ResetPerformance(long id)
        {
            var performance = await musicDb.Performances.SingleOrDefaultAsync(x => x.Id == id);
            if (performance != null)
            {
                var works = performance.Movements.Select(m => m.Work).Distinct();
                foreach (var work in works)
                {
                    await ResetWork(work.Id);
                }
            }
            log.Information($"[A-{performance.Composition.Artist.Id}] {performance.Composition.Artist.Name}, [C-{performance.Composition.Id}] {performance.Composition.Name}, [P-{performance.Id}] {performance.GetAllPerformersCSV()} reset");
            return SuccessResult();
        }
        [HttpGet("repair/performers")]
        public async Task<IActionResult> RepairPerformers()
        {
            SqlServerRetryingExecutionStrategy? strategy = musicDb.Database.CreateExecutionStrategy() as SqlServerRetryingExecutionStrategy;
            if (strategy != null)
            {
                await strategy.ExecuteAsync(async () =>
                {
                    try
                    {
                        using (var tran = musicDb.Database.BeginTransaction())
                        {
                            var allPerformers = musicDb.Performers.ToArray();
                            // first find all names that are duplicated and remove all but one
                            var duplicates = allPerformers
                                .GroupBy(x => x.Name, new AccentAndCaseInsensitiveComparer())
                                .Select(g => new { name = g.Key, performers = g.Select(x => x), Count = g.Count() });
                            foreach (var item in duplicates.Where(x => x.Count > 1))
                            {
                                var highest = (PerformerType)item.performers.Max(x => (int)x.Type);
                                var retained = item.performers.Single(x => x.Type == highest);
                                var replaced = item.performers.Where(x => x.Type != highest);
                                foreach (var r in replaced)
                                {
                                    await musicDb.ReplacePerformer(r, retained);
                                }
                            }
                            await musicDb.SaveChangesAsync();
                            allPerformers = musicDb.Performers.ToArray();
                            // second check all names against the alias list
                            foreach (var performer in allPerformers)
                            {
                                var aliasName = musicOptions.ReplaceAlias(performer.Name);
                                if (aliasName != performer.Name)
                                {
                                    if (aliasName.IsEqualIgnoreAccentsAndCase(performer.Name))
                                    {
                                        // change is restricted to case and/or accent differences
                                        var oldName = performer.Name;
                                        performer.Name = aliasName;
                                        log.Information($"[Pf-{performer.Id}] {performer} name changed from {oldName} to {performer.Name}");
                                    }
                                    else
                                    {
                                        var alias = musicDb.GetPerformer(aliasName, performer.Type);
                                        await musicDb.ReplacePerformer(performer, alias);
                                    }
                                }
                            }
                            await musicDb.SaveChangesAsync();
                            tran.Commit();
                        }
                    }
                    catch (Exception xe)
                    {
                        log.Error($"Error {xe.GetType().Name} thrown within execution strategy");
                        throw;
                    }
                });
            }
            return new EmptyResult();
        }
        [HttpGet("start/musicfilescanner")]
        public async Task<IActionResult> StartMusicFileScanner()
        {
            log.Information("Music scanner started");

            //foreach (var si in new MusicStyleCollection(musicOptions))
            //{
            //    var works = musicDb.Works.Where(x => x.StyleId == si.Style);
            //    foreach (var work in works)
            //    {
            //        var workFiles = work.Tracks.SelectMany(x => x.MusicFiles)
            //            .Where(mf => mf.IsGenerated == false)
            //            .ToArray();
            //        if (workFiles.All(mf => System.IO.File.Exists(mf.File) == false))
            //        {
            //            var roots = workFiles.Select(x => x.GetRootPath()).Distinct(StringComparer.CurrentCultureIgnoreCase);
            //            foreach (var root in roots)
            //            {
            //                //var task = CreateTask(db, style, TaskType.DeletedPath, root);
            //            }
            //        }
            //    } 
            //}
            foreach (var si in new MusicStyleCollection(musicOptions))
            {
                await taskPublisher.AddTask(si.Style);
            }
           
            return new EmptyResult();
        }
        [HttpGet("validate/database")]
        [HttpGet("v/d")]
        public IActionResult ValidateDatabase()
        {
            musicDb.Validate();
            return new EmptyResult();
        }
        [HttpGet("reset/database/{startscan?}")]
        public async Task<IActionResult> ResetDatabase(bool startscan = true)
        {
            log.Warning("Music database being deleted ...");
            await musicDb.Database.EnsureDeletedAsync();
            await musicDb.Database.MigrateAsync();
            log.Warning("Music database deleted and recreated (empty)");
            if (startscan)
            {
                await StartMusicFileScanner();
            }
            return new EmptyResult();
        }
        [HttpGet("resample")]
        public async Task TestResample()
        {
            //D:\temp\source
            //D:\temp\dest
            var sources = Directory.EnumerateFiles(@"D:\temp\source", "*.flac");
            var source = sources.First();
            var destination = Path.Combine(@"D:\temp\dest", Path.GetFileNameWithoutExtension(source) + ".mp3");
            var resampler = new FlacResampler();
            await resampler.Resample(source, destination);
        }
        [HttpGet("test/{n}")]
        public IActionResult Test(int n)
        {
            var folder = $@"D:\Music\flac\Western\Classical\Johannes Brahms\Complete Works\CD{n.ToString("00")}";
            var l1 = new List<string>();
            var sw1 = new Stopwatch();
            sw1.Start();
            foreach (var file in System.IO.Directory.EnumerateFiles(folder, "*.flac"))
            {
                //Debug.WriteLine($"{file}");

                using (var flacFile = new FlacLibSharp.FlacFile(file))
                {
                    var vorbisComment = flacFile.VorbisComment;
                    if (vorbisComment != null)
                    {
                        foreach (var tag in vorbisComment)
                        {
                            var values = string.Join("|", tag.Value.Select(x => x.Trim()).ToArray());
                            l1.Add(tag.Key);
                        }
                    }
                }
            }
            sw1.Stop();
            Debug.WriteLine($"{l1.Count()} read in {sw1.ElapsedMilliseconds} ms");
            var l2 = new List<string>();
            var sw2 = new Stopwatch();
            sw2.Start();
            foreach (var file in System.IO.Directory.EnumerateFiles(folder, "*.flac"))
            {
                //Debug.WriteLine($"{file}");

                var t = new Music.MediaTools.FlacTools(file);
                var vorbisComment = t.GetVorbisCommenTs();
                if (vorbisComment != null)
                {
                    foreach (var kvp in vorbisComment.comments)
                    {
                        l2.Add(kvp.Key);
                    }
                }
            }
            sw2.Stop();
            Debug.WriteLine($"{l2.Count()} read in {sw2.ElapsedMilliseconds} ms");
            return new EmptyResult();
        }
        [HttpGet("metatest/{n}")]
        public async Task<IActionResult> MetaTest(int n)
        {
            switch (n)
            {
                case 1:
                    await CataloguePopular();
                    break;
                case 2:
                    await CatalogueWesternClassical(/*@"Pyotr Il'yich Tchaikovsky"*/);
                    break;
                case 3:
                    GetPopularArtists();
                    break;
                case 4:
                    GetSelectedPopularArtists();
                    break;
                case 5:
                    GetSelectedWesternClassicalArtists();
                    break;
                case 6:
                    await CatalogueJoanBaez();
                    break;
                case 7:
                    await AddPortraitsTask(MusicStyles.Popular);
                    break;
                case 8:
                    Catalogue();
                    break;
                case 9:
                    await CatalogueFolder(MusicStyles.WesternClassical, @"D:\Music\flac\Western\Classical\Wolfgang Amadeus Mozart\Concerto for Flute and Harp");
                    break;
                case 10:
                    await TestPopularAlbumTEO();
                    break;
                //case 11:
                //    await TestWesternClassicalAlbumTEO();
                //    break;
            }

            return new EmptyResult();
        }
        private async Task TestPopularAlbumTEO()
        {
            var work = await musicDb.Works.FirstAsync(w => w.StyleId == MusicStyles.Popular);
            if(work != null)
            {
                var teo = await work.ToPopularAlbumTEO(musicOptions);
                
            }
        }
        //private async Task TestWesternClassicalAlbumTEO()
        //{
        //    var work = await musicDb.Works.FirstAsync(w => w.StyleId == MusicStyles.WesternClassical);
        //    if (work != null)
        //    {
        //        var teo = await work.ToWesternClassicalAlbumTEO(musicOptions);
        //        await teo.SaveMusicTags();
        //        var text = await teo.TestTags();
        //        var teo2 = text.ToInstance<WesternClassicalAlbumTEO>();
        //    }
        //}
        private async Task CatalogueJoanBaez(string artistName)
        {
            await taskPublisher.AddTask(MusicStyles.Popular, "Joan Baez",   true);
        }
        private async Task  CatalogueFolder(MusicStyles style, string path)
        {
            //D:\Music\flac\Western\Classical\Wolfgang Amadeus Mozart\Concerto for Flute and Harp
            await taskPublisher.AddTask(style, TaskType.DiskPath, path, true);
        }
        private async Task AddPortraitsTask(MusicStyles style)
        {
            await taskPublisher.AddPortraitsTask(style);
        }
        private async Task CatalogueJoanBaez()
        {
            await taskPublisher.AddTask(MusicStyles.Popular, "Joan Baez", force: true);
        }
        private void Catalogue()
        {
            void ListFiles(OpusFolder folder)
            {
                var files = folder.GetFilesOnDisk();
                var partCount = files.Where(x => x.part != null).Select(x => x.part.Number).Distinct().Count();
                log.Information($"{(folder.IsCollection ? "Collection" : folder.ArtistName)}, {folder.OpusName}, {folder.Source}, {files.Count()} files in {partCount} parts {(folder.IsGenerated ? ", (generated)" : "")}");
            }
            var names = new string[]
            {
                @"D:\Music\flac\Western\Popular\Bruce Springsteen",
                @"D:\Music\flac\Western\Popular\Elton John",
                @"D:\Music\flac\Western\Popular\Bruce Springsteen\Born In The USA",
                @"D:\Music\flac\Western\Popular\Bruce Springsteen\The River",
                @"D:\Music\flac\Western\Popular\Collections",
                @"D:\Music\flac\Western\Opera\Collections\The Essential Maria Callas",
                @"D:\Music\flac\Western\Classical"
            };
            foreach (var name in names)
            {                
                var pd = MusicMetaDataMethods.GetPathData(musicOptions, name);
                if(pd?.OpusPath != null)
                {
                    // single opus folder
                    var folder = new OpusFolder(musicOptions, pd);
                    ListFiles(folder);
                }
                else if (pd?.OpusPath == null && (pd?.IsCollections ?? false))
                {
                    var cf = new CollectionsFolder(musicOptions, pd.MusicStyle);
                    foreach (var folder in cf.GetOpusFolders())
                    {
                        ListFiles(folder);
                    }
                }
                else if (pd?.OpusPath == null && pd?.ArtistPath != null)
                {
                    // artist folder
                    var af = new ArtistFolder(musicOptions, pd.MusicStyle, pd.ArtistPath);
                    foreach (var folder in af.GetOpusFolders(name))
                    {
                        ListFiles(folder);
                    }
                }
                else if(pd != null)
                {
                    //style folder
                    foreach(var af in pd.MusicStyle.GetArtistFolders(musicOptions))
                    {
                        foreach (var folder in af.GetOpusFolders(name))
                        {
                            ListFiles(folder);
                        }
                    }
                    foreach(var folder in pd.MusicStyle.GetCollectionsFolder(musicOptions).GetOpusFolders(name))
                    {
                        ListFiles(folder);
                    }
                }
            }

        }
        private void GetSelectedWesternClassicalArtists()
        {
            var names = new string[]
                {
                    //@"Franz Schubert",
                    //@"Gabriel Faure",
                    //@"Johann Sebastian Bach",
                    //@"Antonio Vivaldi",
                    //@"Johann Strauss II",
                    @"Pyotr Il'yich Tchaikovsky"
                };
            GetSelectedArtists(MusicStyles.WesternClassical, names);
        }
        private void GetSelectedPopularArtists()
        {
            var names = new string[]
                {
                    @"Bill Withers",
                    @"Buddy Guy",
                    @"David Bowie",
                    @"Blood Sweat & Tears"
                };
            GetSelectedArtists(MusicStyles.Popular, names);
        }
        private void GetSelectedArtists(MusicStyles musicStyle, IEnumerable<string> names)
        {
            foreach (var name in names)
            {
                var af = new ArtistFolder(musicOptions, musicStyle, name);
                var opusFolders = af.GetOpusFolders();
                if (musicStyle == MusicStyles.Popular)
                {
                    log.Information($"{af} has {opusFolders.Count()} album folders (of which {opusFolders.Where(f => f.ForSinglesOnly).Count()} are for singles only");
                }
                else
                {
                    log.Information($"{af} has {opusFolders.Count()} album folders");
                }
                foreach (var wf in opusFolders)
                {
                    var files = wf.GetFilesOnDisk();
                    if (files.Count() > 0)
                    {
                        log.Information($"   {wf}, {files.Count()} music files");
                    }
                }
            }
        }
        private void GetPopularArtists()
        {
            var list = MusicStyles.Popular.GetArtistFolders(musicOptions);
            foreach(var item in list)
            {
                var opusFolders = item.GetOpusFolders();
                log.Information($"{item} has {opusFolders.Count()} album folders (of which {opusFolders.Where(f => f.ForSinglesOnly).Count()} are for singles only");
            }
        }
        private async Task CataloguePopular()
        {
            await taskPublisher.AddTask(MusicStyles.Popular);
        }
        private async Task CatalogueWesternClassical(/*string name*/)
        {
            await taskPublisher.AddTask(MusicStyles.WesternClassical/*, name*/);
        }
        private IActionResult GetImageResult(Image image)
        {
            var ms = new MemoryStream(image.Data);
            return CacheableResult(new FileStreamResult(ms, image.MimeType), image.LastModified);
        }
        private bool CheckDbIntegrity(MusicDb db)
        {
            var result = true;
            var artistsWithoutWorks = db.Artists.Where(a => a.Works.Count() == 0);
            if (artistsWithoutWorks.Count() > 0)
            {
                // result = false;
                // artist without works is possible in the case
                // where a collection has been recorded as a work and the individual artists
                // may not have works themselves

                foreach (var artist in artistsWithoutWorks)
                {
                    log.Trace($"Artist {artist.Name} has no works");
                }
            }
            var worksWithoutTracks = db.Works.Where(a => a.Tracks.Count() == 0);
            if (worksWithoutTracks.Count() > 0)
            {
                result = false;
                foreach (var work in worksWithoutTracks)
                {
                    log.Error($"Artist(s) {work.GetArtistNames()}, work {work.Name} has no tracks");
                }
            }
            var tracksWithoutMusic = db.Tracks.Where(a => a.MusicFiles.Count() == 0);
            if (tracksWithoutMusic.Count() > 0)
            {
                result = false;
                foreach (var track in tracksWithoutMusic)
                {
                    log.Error($"Artist(s) {track.Work.GetArtistNames()}, work {track.Work.Name}, track {track.Title}  has no tracks");
                }
            }
            var tracksWithOnlyGeneratedMusic = db.Tracks.Where(a => a.MusicFiles.All(x => x.IsGenerated));
            if (tracksWithOnlyGeneratedMusic.Count() > 0)
            {
                result = false;
                foreach (var track in tracksWithOnlyGeneratedMusic)
                {
                    log.Error($"Artist(s) {track.Work.GetArtistNames()}, work {track.Work.Name}, track {track.Title} only has generated music");
                }
            }
            var compositionsWithoutPerformances = db.Compositions.Where(a => a.Performances.Count() == 0);
            if (compositionsWithoutPerformances.Count() > 0)
            {
                result = false;
                foreach (var composition in compositionsWithoutPerformances)
                {
                    log.Error($"Artist {composition.Artist.Name}, composition {composition.Name},  has no performances");
                }
            }
            var performancesWithoutMovements = db.Performances.Where(a => a.Movements.Count() == 0);
            if (performancesWithoutMovements.Count() > 0)
            {
                result = false;
                foreach (var performance in performancesWithoutMovements)
                {
                    log.Error($"Artist {performance.Composition.Artist.Name}, composition {performance.Composition.Name},  {performance.GetAllPerformersCSV()} has no movemenents");
                }
            }
            var performancesWhereMovementsAreInvalid = db.Performances.Where(p => p.Movements.Where(m => m.Performance != p).Count() > 0);
            if (performancesWhereMovementsAreInvalid.Count() > 0)
            {
                result = false;
                foreach (var performance in performancesWhereMovementsAreInvalid)
                {
                    log.Error($"Artist {performance.Composition.Artist.Name}, composition {performance.Composition.Name},  {performance.GetAllPerformersCSV()} has suspect movements");
                }
            }
            return result;
        }
        // move this to Fastnet.Core
        private string GetPackageVersion()
        {
            var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            return System.Diagnostics.FileVersionInfo.GetVersionInfo(assemblyLocation).ProductVersion;
        }
    }
}