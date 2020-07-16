using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Core.Web.Controllers;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Metatools;
using Fastnet.Music.Resampler;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
//using System.Web.Http;

namespace Fastnet.Apollo.Web.Controllers
{
    [ServiceFilter(typeof(WebServiceCallTrace))]
    [Route("lib")]
    [ApiController]
    public class LibraryController : BaseController
    {
        private static readonly Regex ipadRegex = new Regex(@"(android|ipad|playbook|silk|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
        private IOptionsMonitor<MusicOptions> musicOptions;
        //private readonly string contentRootPath;
        private readonly IOptionsMonitor<MusicServerOptions> musicServerOptionsMonitor;
        private readonly MusicDb musicDb;
        //private readonly TaskRunner taskRunner;
        private readonly TaskPublisher taskPublisher;
        //private IndianClassicalInformation ici;
        //private readonly IOptionsMonitor<IndianClassicalInformation> monitoredIci;
        public LibraryController(IWebHostEnvironment env,
            IOptionsMonitor<MusicServerOptions> mso,
            TaskPublisher tp,  /*TaskRunner tr,*/
            IOptionsMonitor<MusicOptions> mo, ILogger<LibraryController> logger, MusicDb mdb) : base(logger, env)
        {
            this.musicOptions = mo;//.CurrentValue;
            this.log = logger;
            this.musicServerOptionsMonitor = mso;
            this.musicDb = mdb;
            this.taskPublisher = tp;
            //this.taskRunner = tr;
        }
        [HttpGet("parameters/get/{key?}")]
        public IActionResult GetParameters()
        {
            //Debug.Assert(key != "undefined");
            var clientIPAddress = this.Request.HttpContext.GetRemoteIPAddress();
            if (clientIPAddress == "::1")
            {
                clientIPAddress = NetInfo.GetLocalIPAddress().ToString();
            }
            var key = GetBrowserKey(clientIPAddress);
            var styles = this.musicOptions.CurrentValue.Styles.Select(s => new StyleDTO
            {
                Id = s.Style,
                Enabled = s.Enabled,
                DisplayName = s.Style.ToDescription()
                //Totals = GetStyleTotals(s.Style)

            }).ToArray();
            var dto = new ParametersDTO
            {
                Version = $"{GetPackageVersion()} [{GetAssemblyVersion()}]",
                BrowserKey = key,
                AppName = this.environment.IsDevelopment() ? "Apollo Dev" : "Apollo",
                IsMobile = this.Request.IsMobileBrowser(),
                IsIpad = this.Request.IsIpad(),
                Browser = this.Request.GetBrowser().ToString(),
                ClientIPAddress = clientIPAddress,
                CompactLayoutWidth = this.musicServerOptionsMonitor.CurrentValue.CompactLayoutWidth,
                Styles = styles
            };

            return SuccessResult(dto);
        }
        private string GetBrowserKey(string clientIPAddress)
        {
            var key = string.Empty;
            IEnumerable<Device> devices = musicDb.Devices.Where(d => d.Type == AudioDeviceType.Browser && d.HostMachine == clientIPAddress);
            if(devices.Count() > 1)
            {
                var toRemove = devices.OrderByDescending(x => x.LastSeenDateTime).Skip(1).ToArray();
                foreach(var d in toRemove)
                {
                    log.Information($"device {d.Id}, key {d.KeyName}, {d.HostMachine} {d.DisplayName} is a duplicate - removed");
                }
                musicDb.Devices.RemoveRange(toRemove);
                musicDb.SaveChanges();
            }
            var device = musicDb.Devices.SingleOrDefault(d => d.Type == AudioDeviceType.Browser && d.HostMachine == clientIPAddress);
            if(device == null)
            {
                key = Guid.NewGuid().ToString().ToLower();
                log.Information($"new browser key {key} allocated to {clientIPAddress}");
            }
            else
            {
                key = device.KeyName;
            }
            return key;
        }
        [HttpGet("music/options")]
        public IActionResult GetMusicOptions()
        {
            return SuccessResult(musicOptions.CurrentValue);
        }
        [HttpGet("information/{style}")]
        public IActionResult GetStyleInformation(MusicStyles style)
        {
            (string, string) defaultTotals(IEnumerable<Artist> artists)
            {
                var albums = artists.SelectMany(x => x.ArtistWorkList.Select(x => x.Work));
                var tracks = albums.SelectMany(x => x.Tracks).ToArray();
                var duration = TimeSpan.FromMilliseconds(tracks.Select(t => t.GetBestQuality()).Sum(mf => mf.Duration ?? 0));
                return ($"{artists.Count()} artists, {albums.Count()} works, {tracks.Count()} tracks",  $"{duration.ToDefault()}");
            }
            (string, string) westernClassicalTotals(IEnumerable<Artist> artists)
            {
                var compositions = artists.SelectMany(a => a.Compositions);
                var performances = compositions.SelectMany(c => c.Performances);
                var movements = performances.SelectMany(p => p.Movements);
                var duration = TimeSpan.FromMilliseconds(movements.Select(t => t.GetBestQuality()).Sum(mf => mf.Duration ?? 0));
                return ($"{artists.Count()} artists, {compositions.Count()} compositions, {performances.Count()} performances", $"{duration.ToDefault()}");
            }
            (string, string) indianClassicalTotals(IEnumerable<RagaPerformance> rpList)
            {
                var artists = rpList.Select(rp => rp.Artist).Distinct();
                var ragas = rpList.Select(rp => rp.Raga).Distinct();
                var performances = rpList.Select(rp => rp.Performance).Distinct();
                var movements = performances.SelectMany(p => p.Movements);
                var duration = TimeSpan.FromMilliseconds(movements.Select(t => t.GetBestQuality()).Sum(mf => mf.Duration ?? 0));
                return ($"{artists.Count()} artists, {ragas.Count()} ragas, {performances.Count()} performances", $"{duration.ToDefault()}");
            }
            var result = new List<string>();
            var artists = musicDb.ArtistStyles.Where(x => x.StyleId == style).Select(x => x.Artist);
            string a,  b;
            switch(style)
            {
                default:
                case MusicStyles.Popular:
                    (a, b) = defaultTotals(artists);
                    break;
                case MusicStyles.WesternClassical:
                    (a, b) = westernClassicalTotals(artists);
                    break;
                case MusicStyles.IndianClassical:
                    //var ragas = musicDb.Ragas;
                    (a, b) = indianClassicalTotals(musicDb.RagaPerformances);
                    break;
            }
            result.Add(a);
            result.Add(b);
            //log.Information(result);
            return SuccessResult(result);
        }
        [HttpGet("search/{style}/{searchText}")]
        public async Task<IActionResult> Search(MusicStyles style, string searchText)
        {
            await Task.Delay(0);
            log.Debug($"Search for {searchText} ...");
            var cs = CatalogueSearcher.GetSearcher(this.musicOptions.CurrentValue, style, this.musicDb, this.log);
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
        [HttpGet("get/{style}/artist/{id}")]
        public async Task<IActionResult> GetArtistInfo(MusicStyles style, long id)
        {
            var artist = await musicDb.Artists
                .SingleOrDefaultAsync(x => x.Id == id);
            if (artist == null)
            {
                return ErrorResult($"Artist with id {id} not found");
            }
            else
            {
                return SuccessResult(artist.ToDTO(style));
            }
        }
        [HttpGet("get/{style}/artistSet")]
        public IActionResult GetArtistInfo(MusicStyles style, [FromQuery(Name="id")] long[] ids)
        {
            //ids = ids.OrderBy(k => k).ToArray();
            var set = new ArtistSet(ids);
            if (style == MusicStyles.IndianClassical)
            {
                //var list = musicDb.GetRagaPerformancesForArtistSet(ids);
                var list = musicDb.GetRagaPerformancesForArtistSet(set);
                var dto = list.ToDTO();
                return SuccessResult(dto);
            }
            else
            {
                return new EmptyResult();
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
                return SuccessResult(performance.ToDTO(performance.GetParentEntityName()));
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
            var result = await performance.ToWesternClassicalAlbumTEO(musicOptions.CurrentValue);
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
            var result = await work.ToPopularAlbumTEO(musicOptions.CurrentValue);
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
            var composition = await musicDb.Compositions.FindAsync(id);
            if (composition != null)
            {
                var performances = composition.Performances;
                if (performances.Count() > 0)
                {
                    var result = performances.Select(x => x.ToDTO(composition.Name))
                        .OrderByDescending(x => x.MovementCount)
                        .ThenBy(x => x.Performers);
                    return SuccessResult(result);
                }
                else
                {
                    log.Error($"No performances found for {composition.Artist.Name} composition {composition.Name}");
                }
            }
            else
            {
                log.Error($"Composition {id} not found");
            }
            return ErrorResult("Composition and/or performances not found");
        }
        [HttpGet("get/{style}/{ragaId}/allperformances/artistSet")]
        public IActionResult GetAllPerformances(MusicStyles style, long ragaId, [FromQuery(Name = "id")] long[] ids)
        {
            var set = new ArtistSet(ids);
            var list = musicDb.GetRagaPerformancesForArtistSet(set)
                .Where(x => x.Raga.Id == ragaId);
            if(list.Count() > 0)
            {
                var raga = list.First().Raga.Name;
                return SuccessResult(list.Select(x => x.Performance.ToDTO(raga)));
            }
            return SuccessResult(null);
        }
        private string[] GetStyleTotals(MusicStyles style)
        {
            (string, string) defaultTotals(IEnumerable<Artist> artists)
            {
                var albums = artists.SelectMany(x => x.ArtistWorkList.Select(x => x.Work));
                var tracks = albums.SelectMany(x => x.Tracks).ToArray();
                var duration = TimeSpan.FromMilliseconds(tracks.Select(t => t.GetBestQuality()).Sum(mf => mf.Duration ?? 0));
                return ($"{artists.Count()} artists, {albums.Count()} works, {tracks.Count()} tracks", $"{duration.ToDefault()}");
            }
            (string, string) westernClassicalTotals(IEnumerable<Artist> artists)
            {
                var compositions = artists.SelectMany(a => a.Compositions);
                var performances = compositions.SelectMany(c => c.Performances);
                var movements = performances.SelectMany(p => p.Movements);
                var duration = TimeSpan.FromMilliseconds(movements.Select(t => t.GetBestQuality()).Sum(mf => mf.Duration ?? 0));
                return ($"{artists.Count()} artists, {compositions.Count()} compositions, {performances.Count()} performances", $"{duration.ToDefault()}");
            }
            (string, string) indianClassicalTotals(IEnumerable<RagaPerformance> rpList)
            {
                var artists = rpList.Select(rp => rp.Artist).Distinct();
                var ragas = rpList.Select(rp => rp.Raga).Distinct();
                var performances = rpList.Select(rp => rp.Performance).Distinct();
                var movements = performances.SelectMany(p => p.Movements);
                var duration = TimeSpan.FromMilliseconds(movements.Select(t => t.GetBestQuality()).Sum(mf => mf.Duration ?? 0));
                return ($"{artists.Count()} artists, {ragas.Count()} ragas, {performances.Count()} performances", $"{duration.ToDefault()}");
            }
            var result = new List<string>();
            var artists = musicDb.ArtistStyles.Where(x => x.StyleId == style).Select(x => x.Artist);
            string a, b;
            switch (style)
            {
                default:
                case MusicStyles.Popular:
                    (a, b) = defaultTotals(artists);
                    break;
                case MusicStyles.WesternClassical:
                    (a, b) = westernClassicalTotals(artists);
                    break;
                case MusicStyles.IndianClassical:
                    //var ragas = musicDb.Ragas;
                    (a, b) = indianClassicalTotals(musicDb.RagaPerformances);
                    break;
            }
            result.Add(a);
            result.Add(b);
            return result.ToArray();
        }
        [HttpGet("get/raga/{id}")]
        public async Task<IActionResult> GetRaga(long id)
        {
            musicDb.ChangeTracker.AutoDetectChangesEnabled = false;
            var raga = await musicDb.Ragas.FindAsync(id);
            return SuccessResult(raga.ToDTO());
        }
        [HttpGet("get/artist/allragas")]
        public IActionResult GetAllRagas([FromQuery(Name = "id")] long[] ids)
        {
            var set = new ArtistSet(ids);
            var list = musicDb.GetRagaPerformancesForArtistSet(set)
                .Select(x => x.Raga).Distinct()
                .OrderBy(r => r.DisplayName)
                ;
            return SuccessResult(list.Select(r => r.ToDTO()));
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
        [HttpGet("get/artist/{style}/allworks/{id}/{full?}")]
        public IActionResult GetAllWorks(MusicStyles style, long id, bool full = false)
        {
            musicDb.ChangeTracker.AutoDetectChangesEnabled = false;
            var works = musicDb.ArtistWorkList.Where(awl => awl.ArtistId == id)
                .Select(x => x.Work)
                .Where(w => w.StyleId == style)
                .AsEnumerable();
            var list = works
                .OrderBy(x => x.Name, new NaturalStringComparer())
                .Select(x => x.ToDTO(full));
            return SuccessResult(list);
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
        [HttpGet("resample/work/{id}")]
        public async Task<IActionResult> ResampleWork(long id)
        {
            var work = await musicDb.Works
                .SingleOrDefaultAsync(x => x.Id == id);
            if (work != null)
            {
                var musicFiles = work.Tracks.SelectMany(t => t.MusicFiles).Where(x => !x.IsGenerated);
                var style = musicFiles.First().Style;
                var artists = musicDb.ArtistWorkList
                    .Where(awl => awl.Work == work).Select(awl => awl.Artist);
                var artistNames = string.Join(", ", artists.Select(a => a.Name));
                if (musicFiles.All(f => f.Encoding == EncodingType.flac))
                {
                    var now = DateTimeOffset.Now;
                    var resamplingTask = new TaskItem
                    {
                        Status = Music.Core.TaskStatus.Pending,
                        Type = TaskType.ResampleWork,
                        CreatedAt = now,
                        ScheduledAt = now,
                        MusicStyle = style,
                        TaskString = work.UID.ToString(),
                        Force = true
                    };
                    await musicDb.TaskItems.AddAsync(resamplingTask);
                    await musicDb.SaveChangesAsync();
                    log.Information($"{artistNames}, {work.ToIdent()} {work.Name} forcibly resampled");
                }
            }
            return SuccessResult();
        }
        [HttpGet("resample/performance/{id}")]
        public async Task<IActionResult> ResamplePerformance(long id)
        {
            var performance = await musicDb.Performances.SingleOrDefaultAsync(x => x.Id == id);
            if (performance != null)
            {
                var works = performance.Movements.Select(m => m.Work).Distinct();
                foreach (var work in works)
                {
                    await ResampleWork(work.Id);
                }
            }
            log.Information($"{performance.ToLogIdentity()}  {performance.GetAllPerformersCSV()} resampled");
            return SuccessResult();
        }
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
            log.Information($"{performance.ToLogIdentity()}  {performance.GetAllPerformersCSV()} reset");
            //log.Information($"[A-{performance.Composition.Artist.Id}] {performance.Composition.Artist.Name}, [C-{performance.Composition.Id}] {performance.Composition.Name}, [P-{performance.Id}] {performance.GetAllPerformersCSV()} reset");
            return SuccessResult();
        }
        [HttpGet("repair/performers")]
        public async Task<IActionResult> RepairPerformers()
        {
            SqlServerRetryingExecutionStrategy strategy = musicDb.Database.CreateExecutionStrategy() as SqlServerRetryingExecutionStrategy;
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
                                var aliasName = musicOptions.CurrentValue.ReplaceAlias(performer.Name);
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
        [HttpGet("start/rescan/{style}")]
        public async Task<IActionResult> RescanStyle(MusicStyles style)
        {
            log.Information($"Rescan started for {style}");

            await taskPublisher.AddTask(style);

            return new EmptyResult();
        }
        [HttpGet("start/musicfilescanner")]
        public async Task<IActionResult> StartMusicFileScanner()
        {
            log.Information("Music scanner started");

            foreach (var si in new MusicStyleCollection(musicOptions.CurrentValue))
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
        private IActionResult GetImageResult(Image image)
        {
            var ms = new MemoryStream(image.Data);
            return CacheableResult(new FileStreamResult(ms, image.MimeType), image.LastModified);
        }
        private string GetPackageVersion()
        {
            //var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            //return System.Diagnostics.FileVersionInfo.GetVersionInfo(assemblyLocation).ProductVersion;
            return System.Reflection.Assembly.GetExecutingAssembly().GetPackageVersion();
        }
        private string GetAssemblyVersion()
        {
            //return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            return System.Reflection.Assembly.GetExecutingAssembly().GetAssemblyVersion();
        }
    }
}