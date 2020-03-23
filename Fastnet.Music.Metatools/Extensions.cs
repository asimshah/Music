using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.TagLib;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
//using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using fsl = FlacLibSharp;
//using System.IO;
using IO = System.IO;

namespace Fastnet.Music.Metatools
{
    public static partial class Extensions
    {
        private static readonly ILogger log = ApplicationLoggerFactory.CreateLogger("Fastnet.Music.Metatools.Extensions");
        /// <summary>
        /// returns true if the image does not use the given filename
        /// or the length or last write time have changed
        /// </summary>
        /// <param name="image"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static bool HasChanged(this Image image, string filename)
        {
            if (image == null && filename == null)
            {
                return false;
            }
            if (image == null && filename != null || image != null && filename == null)
            {
                return true;
            }
            var result = true;
            if (image != null && string.Compare(image.Sourcefile, filename, true) == 0)
            {
                var fi = new FileInfo(filename);
                if (fi.Length == image.Filelength && fi.LastWriteTime == image.LastModified)
                {
                    result = false;
                }
            }
            return result;
        }
        public static CollectionsFolder GetCollectionsFolder(this MusicStyles musicStyle, MusicOptions musicOptions)
        {
            return new CollectionsFolder(musicOptions, musicStyle);
        }
        public static IEnumerable<ArtistFolder> GetArtistFolders(this MusicStyles musicStyle, MusicOptions musicOptions, string selectedRootFolder = null)
        {
            var folderList = new List<ArtistFolder>();

            var style = MusicMetaDataMethods.GetStyleInfo(musicOptions, musicStyle);
            if (style != null)
            {
                var list = new List<string>();
                //foreach (var rootFolder in new MusicSources(musicOptions).Where(s => !s.IsGenerated).OrderBy(s => s.DiskRoot))
                foreach (var rootFolder in new MusicSources(musicOptions))
                {
                    foreach (var setting in style.Settings)
                    {
                        var path = Path.Combine(rootFolder.DiskRoot, setting.Path);
                        if (selectedRootFolder == null || path.StartsWithIgnoreAccentsAndCase(selectedRootFolder))
                        {
                            if (Directory.Exists(path))
                            {
                                list.AddRange(Directory.EnumerateDirectories(path).Select(d => Path.GetFileName(d)));
                            }
                        }
                    }
                }
                var list2 = list.Except(new string[] { "collections", "$portraits" }, StringComparer.CurrentCultureIgnoreCase);
                if (style.Filter)
                {
                    list2 = list2.Intersect(style.IncludeArtists, new AccentAndCaseInsensitiveComparer());
                }
                list2 = list2.Distinct(new AccentAndCaseInsensitiveComparer()).OrderBy(x => x);
                folderList = list2.Select(n => new ArtistFolder(musicOptions, musicStyle, n)).ToList();
            }
            return folderList;
        }
        /// <summary>
        /// returns a list of directories found in all sources that match either the artist, or the work
        /// Note that a directory will match if the name matches ignoring accents or case
        /// </summary>
        /// <param name="musicStyle"></param>
        /// <param name="musicOptions"></param>
        /// <param name="artistName"></param>
        /// <param name="workName"></param>
        /// <returns></returns>

        /// <summary>
        /// Reads all the tags in the audio file and adds them to the database
        /// (Not all the tags are used)
        /// </summary>
        /// <param name="mf"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static async Task UpdateTagsAsync(this MusicDb db, MusicFile mf/*, bool force = false*/)
        {
            if (/*force == true*/
                /*||*/ mf.ParsingStage == MusicFileParsingStage.Unknown
                || mf.ParsingStage == MusicFileParsingStage.Initial
                || (mf.FileLastWriteTimeUtc != new IO.FileInfo(mf.File).LastWriteTimeUtc))
            {
                // for western classical the important tags are:
                //      ArtistName, WorkName, Year, Composition, Orchestra, Conductor, Performers, TrackNumber, Title
                //      what about cover art?? can I assume that there will always be an image file on disk somewhere??
                var oldTags = mf.IdTags.ToArray();
                mf.IdTags.Clear();
                db.IdTags.RemoveRange(oldTags);
                switch (mf.Encoding)
                {
                    case EncodingType.flac:
                        db.UpdateFlacTagsAsync(mf);
                        break;
                    default:
                        await db.UpdateMp3TagsAsync(mf);
                        break;
                }
                mf.ParsingStage = MusicFileParsingStage.IdTagsComplete;
            }
        }
        public static string GetArtistName(this MusicFile mf/*, MusicDb db*/)
        {
            var name = mf.Musician;
            switch (mf.Encoding)
            {
                case EncodingType.flac:
                    Debug.Assert(mf.IsGenerated == false);
                    switch (mf.Style)
                    {
                        case MusicStyles.WesternClassical:
                            name = mf.GetTagValue<string>("Composer") ?? mf.GetTagValue<string>("Artist") ?? name;
                            break;
                        default:
                            name = mf.GetTagValue<string>("Artist") ?? name;
                            break;
                    }
                    break;
                default:
                    if (mf.IsGenerated)
                    {
                        name = mf.GetTagValue<string>("Artist");
                    }
                    else
                    {
                        name = mf.GetTagValue<string>("Artist") ?? mf.GetTagValue<string>("AlbumArtists") ?? name;
                        name = name.Split('|', ';', ',', ':')[0].Trim();
                    }
                    break;
            }
            return name;
        }
        public static string GetWorkName(this MusicFile mf/*, MusicDb db*/)
        {
            var name = mf.OpusName;
            if (!(mf.OpusType == OpusType.Singles))
            {
                switch (mf.Encoding)
                {
                    case EncodingType.flac:
                    default:
                        //Debug.Assert(mf.IsGenerated == false);
                        switch (mf.Style)
                        {
                            case MusicStyles.WesternClassical:
                                name = mf.GetTagValue<string>("Composition") ?? mf.GetTagValue<string>("Album") ?? name;
                                break;
                            default:
                                name = mf.GetAlbumName();// mf.GetTagValue<string>("Album") ?? name;
                                break;
                        }
                        break;
                }
            }
            return name;
        }
        public static string GetAlbumName(this MusicFile mf/*, MusicDb db*/)
        {
            var name = mf.OpusName;
            if (!(mf.OpusType == OpusType.Singles))
            {
                name = mf.GetTagValue<string>("Album") ?? name;
            }
            return name;
        }
        public static int? GetYear(this MusicFile mf)
        {
            int? year;
            switch (mf.Encoding)
            {
                case EncodingType.flac:
                    Debug.Assert(mf.IsGenerated == false);
                    //year = mf.GetTagIntValue("Date") ?? mf.GetTagIntValue("OriginalDate") ?? mf.GetTagIntValue("OriginalYear") ?? 0;
                    year = mf.GetTagIntValue("OriginalYear") ?? mf.GetTagIntValue("OriginalDate") ?? mf.GetTagIntValue("Date") ?? 0;
                    break;
                default:
                    year = mf.GetTagIntValue("OriginalYear") ?? mf.GetTagIntValue("Year") ?? 0;
                    break;
            }
            return year;
        }
        public static IEnumerable<string> GetPerformers(this MusicFile mf)
        {
            var performerText = mf.GetTagValue<string>("Performer")
                ?? mf.GetTagValue<string>("Performers")
                ?? string.Empty;
            if (mf.MusicianType != ArtistType.Various)
            {
                var artistText = $"{mf.GetTagValue<string>("Album Artist") ?? string.Empty}|{mf.GetTagValue<string>("AlbumArtist") ?? string.Empty}";
                if (artistText.Length > 0)
                {
                    performerText = $"{performerText}|{artistText}";
                }
            }
            return performerText.Split(new char[] { '|', ';', ',', ':' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => Regex.Replace(t, @"\(.*?\)", "").Trim());
        }
        public static IEnumerable<string> GetOrchestras(this MusicFile mf)
        {
            return GetSplittableTag(mf, "Orchestra");
            //return mf.GetTagValue<string>("Orchestra");
        }
        public static IEnumerable<string> GetConductors(this MusicFile mf)
        {
            return GetSplittableTag(mf, "Conductor");
            //var conductor = mf.GetTagValue<string>("Conductor");
            //if (conductor != null && conductor.Contains('|'))
            //{
            //    conductor = conductor.Split('|', StringSplitOptions.RemoveEmptyEntries).First();
            //}
            //return conductor;// mf.GetTagValue<string>("Conductor");
        }
        private static IEnumerable<string> GetSplittableTag(this MusicFile mf, string tagName)
        {
            var r = mf.GetTagValue<string>(tagName);
            if (r != null)
            {
                return r.Split('|', StringSplitOptions.RemoveEmptyEntries);
            }
            return new string[0];// mf.GetTagValue<string>("Conductor");
        }
        public static bool ValidateTags(this MusicDb db, MusicFile mf)
        {
            bool result = true;
            bool isTagPresent(string tagName, bool logIfMissing = false)
            {
                var r = mf.IdTags.SingleOrDefault(x => string.Compare(x.Name, tagName, true) == 0) != null;
                if (!r && logIfMissing)
                {
                    log.Error($"{mf.File} tag {tagName} not present");
                }
                return r;
            }
            if (mf.IsGenerated)
            {
                result = true;
            }
            else
            {
                var standardTags = new string[] { "Artist", "Album", "TrackNumber", "Title" };
                if (standardTags.Any(t => isTagPresent(t, true) == false))
                {
                    result = false;
                }
                else
                {
                    switch (mf.Style)
                    {
                        case MusicStyles.WesternClassical:
                            // optional tags: these do  not need to be preent but we report if they are not
                            var optionalTags = new string[] { "Composer", "Composition", "Conductor", "Orchestra" };
                            var missingOptional = optionalTags.Where(t => isTagPresent(t) == false).ToList();
                            missingOptional.ForEach(t => log.Trace($"{mf.File} optional tag {t} not present"));
                            // alternate tags: atleast one of these neeeds to be present
                            var alternateTags = new string[] { "Performer", "Album Artist", "AlbumArtist", "AlbumArtists" };
                            if (!alternateTags.Any(t => isTagPresent(t)))
                            {
                                result = false;
                                log.Error($"{mf.File} none of {(string.Join(", ", alternateTags))} found");
                            }
                            break;
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// get the most applicable cover file for the work, according to the rules
        /// i) jpg is preferred to jpeg which is preferred to png (.xxx below)
        /// ii) *cover.xxx is preferred to *front.xxx which is preferred *folder.xxx
        /// </summary>
        /// <param name="work"></param>
        /// <param name="musicOptions"></param>
        /// <param name="musicStyle"></param>
        /// <param name="folderName"></param>
        /// <returns></returns>
        public static string GetCoverFile(this Work work, MusicOptions musicOptions, string folderName = null)
        {
            folderName ??= work.Name;
            string coverFile = null;
            //var allworkFolders = work.StyleId.FindAllDirectories(musicOptions, work.Artist.Name, work.Name);
            var allworkFolders = work.StyleId.GetOpusFolders(musicOptions, work.Artist.Type, work.Artist.Name, work.Name);

            foreach (var pattern in musicOptions.CoverFilePatterns)
            {
                coverFile = allworkFolders.SelectMany(p => IO.Directory.EnumerateFiles(p, pattern, IO.SearchOption.AllDirectories)).FirstOrDefault(cf => IO.File.Exists(cf));
                if (coverFile != null)
                {
                    break;
                }
            }
            return coverFile;
        }
        public static string GetPortraitFile(this Artist artist, MusicOptions musicOptions)
        {
            var ln = artist.Name.GetLastName();
            bool matchImageFilename(string imageFileName, string artistName)
            {
                var imagename = Path.GetFileNameWithoutExtension(imageFileName).ToLower();
                return "portrait" == imagename || artist.Name.IsEqualIgnoreAccentsAndCase(imagename) || ln.IsEqualIgnoreAccentsAndCase(imagename);
            }
            bool matchArtistFolder(string sp, string artistName, string folder)
            {
                bool _match(string sp, string name)
                {
                    var artistFolder = Path.Combine(sp, name);
                    if (artistFolder.IsEqualIgnoreAccentsAndCase(folder))
                    {
                        return true;
                    }
                    return false;
                }
                if (_match(sp, artistName))
                {
                    return true;
                }
                else
                {
                    if (artistName.StartsWith("The ", StringComparison.CurrentCultureIgnoreCase))
                    {
                        artistName = artistName.Substring(4);
                    }
                    else
                    {
                        artistName = $"The {artistName}";
                    }
                    return _match(sp, artistName);
                }
            }
            var allStylePaths = artist.ArtistStyles.ToArray()
                .SelectMany(x => x.StyleId.GetPaths(musicOptions, false, false));
            var allArtistPaths = allStylePaths.SelectMany(
                sp => Directory.EnumerateDirectories(sp)
                .Where(d => matchArtistFolder(sp, artist.Name, d) || Path.GetFileNameWithoutExtension(d).ToLower() == "$portraits"));
            var imageFiles = allArtistPaths.SelectMany(x => Directory.EnumerateFiles(x, "*.jpg")
                .Union(Directory.EnumerateFiles(x, "*.jpeg"))
                .Union(Directory.EnumerateFiles(x, "*.png")));
            var matchedFiles = imageFiles.Where(f => matchImageFilename(f, artist.Name));
            return matchedFiles.OrderByDescending(x => new FileInfo(x).LastWriteTime).FirstOrDefault();
        }

        public static async Task<Image> GetImage(this string filename)
        {
            (var data, var mimeType, var lastWriteTimeUtc, var length) = await GetImageDetails(filename);
            var image = new Image
            {
                Sourcefile = filename,
                Filelength = length,
                LastModified = lastWriteTimeUtc,
                Data = data,
                MimeType = mimeType
            };
            return image;
        }
        public static void Delete(this MusicDb musicDb, MusicFile mf, DeleteContext context)
        {
            void clearRelatedEntities(MusicFile file)
            {
                musicDb.RemovePlaylistItems(file);
                var tags = file.IdTags.ToArray();
                musicDb.IdTags.RemoveRange(tags);
            }
            clearRelatedEntities(mf);
            var track = mf.Track;
            track?.MusicFiles.Remove(mf);
            if (track?.MusicFiles.All(x => x.IsGenerated) ?? false)
            {
                foreach (var f in track.MusicFiles.ToArray())
                {
                    clearRelatedEntities(f);
                    f.Track = null;
                    track.MusicFiles.Remove(f);
                    musicDb.MusicFiles.Remove(f);
                    log.Information($"{context}: Musicfile [MF-{f.Id}] deleted: {f.File}");
                }
            }
            if (track?.MusicFiles.Count() == 0)
            {
                musicDb.Delete(track, context);
            }
            musicDb.MusicFiles.Remove(mf);
            log.Information($"{context}: Musicfile [MF-{mf.Id}] deleted: {mf.File}");
        }
        public static void RemovePlaylistItems<T>(this MusicDb musicDb, T entity)
        {
            PlaylistItemType itemType = PlaylistItemType.MusicFile;
            long itemId = 0;
            switch (entity)
            {
                case Performance p:
                    itemType = PlaylistItemType.Performance;
                    itemId = p.Id;
                    break;
                case Work w:
                    itemType = PlaylistItemType.Work;
                    itemId = w.Id;
                    break;
                case Track t:
                    itemType = PlaylistItemType.Track;
                    itemId = t.Id;
                    break;
                case MusicFile mf:
                    itemType = PlaylistItemType.MusicFile;
                    itemId = mf.Id;
                    break;
            }
            var items = musicDb.PlaylistItems.Where(x => x.Type == itemType && x.ItemId == itemId).ToArray();
            foreach (var item in items)
            {
                var playlist = item.Playlist;
                item.Playlist = null;
                playlist.Items.Remove(item);
                musicDb.PlaylistItems.Remove(item);
                if (playlist.Items.Count() == 0)
                {
                    musicDb.Playlists.Remove(playlist);
                }
            }
        }
        public static bool ValidateArtists(this MusicDb db)
        {
            var result = true;
            foreach (var artist in db.Artists)
            {
                var styleCount = artist.ArtistStyles.Count();
                var r = styleCount > 0;
                if (!r)
                {
                    log.Warning($"Artist {artist.Name} [A-{artist.Id}] has no artiststyle entries");
                    if (result == true)
                    {
                        result = false;
                    }
                }
                r = artist.Works.Count() > 0 || artist.Compositions.Count() > 0;
                if (!r)
                {
                    log.Warning($"Artist {artist.Name} [A-{artist.Id}] has neither works nor compositions");
                    if (result == true)
                    {
                        result = false;
                    }
                }
            }
            log.Information("ValidateArtists() completed");
            return result;
        }
        public static bool ValidateWorks(this MusicDb db)
        {
            var result = true;
            foreach (var work in db.Works)
            {
                var styles = work.Artist.ArtistStyles.Select(x => x.StyleId);
                var r = styles.Any(x => x == work.StyleId);
                if (!r)
                {
                    log.Warning($"Work {work.Name} [{work.Id}] is in style {work.StyleId} but artist {work.Artist} [W-{work.Artist.Id}] is not");
                    if (result == true)
                    {
                        result = false;
                    }
                }
                r = work.Tracks.Count() > 0;
                if (!r)
                {
                    log.Warning($"Work {work.Name} [W-{work.Id}] has no tracks");
                    if (result == true)
                    {
                        result = false;
                    }
                }
            }
            log.Information("ValidateWorks() completed");
            return result;
        }
        public static bool ValidateTracks(this MusicDb db)
        {
            //var result = true;
            int errorCount = 0;
            foreach (var track in db.Tracks)
            {
                if (track.MusicFiles.Count() == 0)
                {
                    log.Warning($"Track {track.Title} [T-{track.Id}] has no music files");
                    ++errorCount;
                }
                if (track.MusicFiles.All(x => x.IsGenerated))
                {
                    log.Warning($"Track {track.Title} [T-{track.Id}] has only generated music files");
                    ++errorCount;
                }
            }
            log.Information("ValidateTracks() completed");
            return errorCount == 0;
        }
        public static bool ValidateCompositions(this MusicDb db)
        {
            var result = true;
            foreach (var composition in db.Compositions)
            {
                var performanceCount = composition.Performances.Count();
                var r = performanceCount > 0;
                if (!r)
                {
                    log.Warning($"Composition {composition.Name} [C-{composition.Id}] has no performances");
                    if (result == true)
                    {
                        result = false;
                    }
                }
            }
            log.Information("ValidateCompositions() completed");
            return result;
        }
        public static bool Validate(this MusicDb musicDb)
        {
            var list = new List<bool>() {
                musicDb.ValidateArtists(),
                musicDb.ValidateWorks(),
                musicDb.ValidateTracks(),
                musicDb.ValidateCompositions(),
                musicDb.ValidatePerformances()
            };
            return list.All(x => x == true);
        }
        public static bool ValidatePerformances(this MusicDb db)
        {
            var result = true;
            foreach (var performance in db.Performances)
            {
                var movementCount = performance.Movements.Count();
                var r = movementCount > 0;
                if (!r)
                {
                    log.Warning($"{performance.Composition.Artist.Name} [A-{performance.Composition.Artist.Id}], \"{performance.Composition.Name}\" [C-{performance.Composition.Id}] performed by \"{performance.GetAllPerformersCSV()}\" [P-{performance.Id}] has no movements");
                    if (result == true)
                    {
                        result = false;
                    }
                }
                if (movementCount > 0)
                {
                    var workCount = performance.Movements.Select(x => x.Work).Distinct().Count();
                    r = workCount == 1;
                    if (!r)
                    {
                        log.Warning($"{performance.Composition.Artist.Name} [A-{performance.Composition.Artist.Id}], \"{performance.Composition.Name} [C-{performance.Composition.Id}] movements\" in performance by {performance.GetAllPerformersCSV()} [P-{performance.Id}] have a work count of {workCount}");
                        if (result == true)
                        {
                            result = false;
                        }
                    }
                }
            }
            log.Information("ValidatePerformances() completed");
            return result;
        }
        private static async Task<(byte[] data, string mimeType, DateTimeOffset lastWriteTimeUtc, long length)> GetImageDetails(string imageFile)
        {
            var fi = new IO.FileInfo(imageFile);
            var data = await IO.File.ReadAllBytesAsync(fi.FullName);
            var mimeType = string.Empty;

            switch (fi.Extension.ToLower())
            {
                case ".jpeg":
                    mimeType = "image/jpeg";
                    break;
                case ".jpg":
                    mimeType = "image/jpg";
                    break;
                case ".png":
                    mimeType = "image/png";
                    break;
            }
            return (data, mimeType, fi.LastWriteTimeUtc, fi.Length);
        }
        private static IEnumerable<string> GetFoldersAcrossStyles(MusicOptions musicOptions, IEnumerable<MusicStyles> musicStyles, string folderName)
        {
            try
            {
                var stylePaths = new List<string>();
                foreach (var ms in musicStyles)
                {
                    stylePaths.AddRange(ms.GetPaths(musicOptions, false, false));
                }
                var t1 = stylePaths.Select(x => IO.Path.Combine(x, folderName));
                var t2 = t1.Where(x => IO.Directory.Exists(x));
                var t3 = stylePaths.Select(x => IO.Path.Combine(x, folderName))
                    .Where(x => IO.Directory.Exists(x));
                return stylePaths.Select(x => IO.Path.Combine(x, folderName))
                    .Where(x => IO.Directory.Exists(x));
            }
            catch (Exception)
            {
                //Debugger.Break();
                throw;
            }
        }
        private static void Delete(this MusicDb musicDb, Artist artist, DeleteContext context)
        {
            long artistId = artist.Id;
            foreach (var composition in artist.Compositions)
            {
                composition.Artist = null;
                musicDb.Delete(composition, context);
            }
            foreach (var work in artist.Works)
            {
                work.Artist = null;
                musicDb.Delete(work, context);
            }
            var styles = artist.ArtistStyles.ToArray();
            musicDb.ArtistStyles.RemoveRange(styles);
            musicDb.Artists.Remove(artist);
            context.SetDeletedArtistId(artistId);
            log.Information($"{context}: Artist [A-{artist.Id}] deleted: {artist.Name}");
        }
        private static void Delete(this MusicDb musicDb, Composition composition, DeleteContext context)
        {
            long artistId = composition.ArtistId;
            foreach (var performance in composition.Performances)
            {
                performance.Composition = null;
                musicDb.Delete(performance, context);
            }
            composition.Performances.Clear();
            musicDb.Compositions.Remove(composition);
            var artist = composition.Artist;
            artist?.Compositions.Remove(composition);
            if (artist != null)
            {
                if (artist.Works.Count() == 0 && artist.Compositions.Count() == 0)
                {
                    musicDb.Delete(artist, context);
                }
            }
            context.SetModifiedArtistId(artistId);
            log.Information($"{context}: Composition [C-{composition.Id}] deleted: {composition.Name}");
        }
        public static void Delete(this MusicDb musicDb, Performance performance, DeleteContext context)
        {
            long artistId = performance.Composition.ArtistId;
            foreach (var movement in performance.Movements)
            {
                movement.Performance = null;
                // we do not delete movements here because
                // a movement is a track and tracks are also in an album
            }
            performance.Movements.Clear();
            var composition = performance.Composition;
            composition?.Performances.Remove(performance);
            if (composition?.Performances.Count() == 0)
            {
                musicDb.Delete(composition, context);
            }
            var performers = performance.GetAllPerformersCSV();
            musicDb.PerformancePerformers.RemoveRange(performance.PerformancePerformers);
            musicDb.Performances.Remove(performance);
            context.SetModifiedArtistId(artistId);
            log.Information($"{context}: Performance [P-{performance.Id}] deleted: {performers}");
        }
        private static void Delete(this MusicDb musicDb, Work work, DeleteContext context)
        {
            long artistId = work.ArtistId;
            foreach (var track in work.Tracks)
            {
                track.Work = null;
                musicDb.Delete(track, context);
            }
            var artist = work.Artist;
            artist?.Works.Remove(work);
            if (artist != null)
            {
                if (artist.Works.Count() == 0 && artist.Compositions.Count() == 0)
                {
                    musicDb.Delete(artist, context);
                }
            }
            musicDb.Works.Remove(work);
            context.SetModifiedArtistId(artistId);
            log.Information($"{context}: Work [W-{work.Id}] deleted: {work.Name}");
        }
        private static void Delete(this MusicDb musicDb, Track track, DeleteContext context)
        {
            long artistId = track.Work.ArtistId;
            foreach (var musicFile in track.MusicFiles.ToArray())
            {
                musicFile.Track = null;
                musicDb.Delete(musicFile, context);
            }
            var performance = track.Performance;
            performance?.Movements.Remove(track);
            if (performance?.Movements.Count() == 0)
            {
                musicDb.Delete(performance, context);
            }
            var work = track.Work;
            work?.Tracks.Remove(track);
            if (work?.Tracks.Count() == 0)
            {
                musicDb.Delete(work, context);
            }
            musicDb.Tracks.Remove(track);
            context.SetModifiedArtistId(artistId);
            log.Information($"{context}: Track [T-{track.Id}] deleted: {track.Title}");
        }
        private static void UpdateFlacTagsAsync(this MusicDb musicDb, MusicFile mf)
        {
            const string vorbisSeparators = ";\r\n\t";
            using (var file = new fsl.FlacFile(mf.File))
            {
                var vorbisComment = file.VorbisComment;
                if (vorbisComment != null)
                {
                    foreach (var tag in vorbisComment)
                    {
                        try
                        {
                            var values = tag.Value.Select(x => x.Trim()).ToList();
                            if (values.Any(x => x.IndexOfAny(vorbisSeparators.ToCharArray()) >= 0))
                            {
                                var subValues = new List<string>();
                                foreach (var item in values)
                                {
                                    var parts = item.Split(vorbisSeparators.ToCharArray());
                                    subValues.AddRange(parts.Select(x => x.Trim()));
                                }
                                values = subValues;
                            }
                            //var value = string.Join("|", tag.Value.Select(x => x.Trim()).ToArray());
                            var value = string.Join("|", values);
                            var idTag = new IdTag
                            {
                                MusicFile = mf,
                                Name = tag.Key,
                                Value = value
                            };
                            mf.IdTags.Add(idTag);
                            //await musicDb.IdTags.AddAsync(idTag);
                        }
                        catch (Exception xe)
                        {
                            log.Error(xe);
                            throw;
                        }
                    }
                }
                var pictures = file.GetAllPictures();
                var picture = pictures.FirstOrDefault(x => x.PictureType == FlacLibSharp.PictureType.CoverFront);
                if (picture != null)
                {
                    var idTag = new IdTag
                    {
                        MusicFile = mf,
                        Name = "Pictures",
                        PictureMimeType = picture.MIMEType,
                        PictureData = picture.Data
                    };
                    mf.IdTags.Add(idTag);
                    //await musicDb.IdTags.AddAsync(idTag);
                }
                else
                {
                    log.Debug($"{mf.File} {pictures.Count()} pictures found - but no FlacLibSharp.PictureType.CoverFront");
                }
                if (mf.IdTags.SingleOrDefault(x => string.Compare(x.Name, "COMPOSITION", true) == 0) == null)
                {
                    var w = mf.IdTags.SingleOrDefault(x => string.Compare(x.Name, "Work", true) == 0);
                    if (w == null)
                    {
                        var title = mf.IdTags.SingleOrDefault(x => string.Compare(x.Name, "Title", true) == 0);
                        if (title != null)
                        {
                            var parts = title.Value.Split(':');
                            var idTag = new IdTag
                            {
                                MusicFile = mf,
                                Name = "COMPOSITION",
                                Value = parts.First().Trim()
                            };
                            mf.IdTags.Add(idTag);
                            if (parts.Length > 1)
                            {
                                title.Value = string.Join(":", parts.Skip(1)).Trim();
                            }
                            //else
                            //{
                            //    log.Information("pause");
                            //}
                        }
                        //if (title != null && title.Value.Contains(":"))
                        //{
                        //    var parts = title.Value.Split(':');
                        //    var idTag = new IdTag
                        //    {
                        //        MusicFile = mf,
                        //        Name = "COMPOSITION",
                        //        Value = parts.First().Trim()
                        //    };
                        //    mf.IdTags.Add(idTag);
                        //    //await musicDb.IdTags.AddAsync(idTag);
                        //}
                    }
                    else
                    {
                        var work = w.Value.Split(':');
                        var idTag = new IdTag
                        {
                            MusicFile = mf,
                            Name = "COMPOSITION",
                            Value = work.First().Trim()
                        };
                        mf.IdTags.Add(idTag);
                        //await musicDb.IdTags.AddAsync(idTag);
                    }
                }
                if (mf.IdTags.SingleOrDefault(x => string.Compare(x.Name, "Orchestra", true) == 0) == null)
                {
                    //var performers = mf.GetTagValue("Performer") ?? mf.GetTagValue("Performers");
                    var performers = mf.GetTag("Performer") ?? mf.GetTag("Performers");
                    if (performers != null)
                    {
                        var orchestra = Extract("orchestra", performers.Value);
                        if (orchestra != null)
                        {
                            performers.Value = Remove("orchestra", performers.Value);
                            var idTag = new IdTag
                            {
                                MusicFile = mf,
                                Name = "ORCHESTRA",
                                Value = orchestra
                            };
                            mf.IdTags.Add(idTag);
                            //await musicDb.IdTags.AddAsync(idTag);
                        }
                    }
                }
                if (mf.IdTags.SingleOrDefault(x => string.Compare(x.Name, "Conductor", true) == 0) == null)
                {
                    var performers = mf.GetTag("Performer") ?? mf.GetTag("Performers");
                    if (performers != null)
                    {
                        var orchestra = Extract("conductor", performers.Value);
                        if (orchestra != null)
                        {
                            performers.Value = Remove("conductor", performers.Value);
                            var idTag = new IdTag
                            {
                                MusicFile = mf,
                                Name = "CONDUCTOR",
                                Value = orchestra
                            };
                            mf.IdTags.Add(idTag);
                            //await musicDb.IdTags.AddAsync(idTag);
                        }
                    }
                }
            }
        }
        private static async Task UpdateMp3TagsAsync(this MusicDb musicDb, MusicFile mf)
        {
            var tags = new Dictionary<string, object>();
            var file = TagLib.File.Create(mf.File);
            //bool allowStyleTags = true;
            var performers = new List<string>();
            void addPerformers(string[] strings)
            {
                foreach (var s in strings)
                {
                    string p = s.CleanName();
                    var parts = p.Split(new char[] { ';', ',', '|', ':' });
                    performers.AddRange(parts);
                }
            }
            var t = file.GetTag(Music.TagLib.TagTypes.AllTags);
            var tag = file.Tag;
            object tt;// = null;
            // first get commontags (ie from all files regardless)
            tt = tag?.Track; if (tt != null) { tags.Add("TrackNumber", tag.Track.ToString()); }
            tt = tag?.Title; if (tt != null) { tags.Add(nameof(tag.Title), tag.Title); }
            tt = tag?.Album; if (tt != null) { tags.Add(nameof(tag.Album), tag.Album); }
            tt = tag?.Composers; if (tt != null) { tags.Add("Composer", string.Join("|", tag.Composers)); }
            try
            {
                tags.Add(nameof(tag.Pictures), tag.Pictures?.FirstOrDefault());
            }
            catch { }

            // now check and get Apollo tags
            Music.TagLib.Id3v2.Tag id3v2 = (Music.TagLib.Id3v2.Tag)file.GetTag(Music.TagLib.TagTypes.Id3v2);
            if (id3v2 != null && id3v2.GetApolloString("ApolloResampled") != null)
            {
                // this is an mp3 file created by an apollo agent (which is the only way this tag can have been written!!)
                // these files are the result of resampling flac files to vbr and as such are always going to be 
                // additional music files for a track.
                var artist = id3v2.GetApolloString("ApolloArtist");
                if (artist != null)
                {
                    tags.Remove("Artist");
                    tags.Add("Artist", artist);
                }
                var album = id3v2.GetApolloString("ApolloAlbum");
                if (album != null)
                {
                    tags.Remove("Album");
                    tags.Add("Album", album);
                }
                var composition = id3v2.GetApolloString("ApolloComposition");
                if (composition != null)
                {
                    tags.Add("Composition", composition);
                }
                // generated files contain a ApolloPerformers tag that includes any Orchestra and Conductor
                // this code copies that into Performer
                var apolloPerformers = id3v2.GetApolloStrings("ApolloPerformers");
                if (apolloPerformers != null)
                {
                    string performersString = string.Join("|", apolloPerformers.Distinct(StringComparer.CurrentCultureIgnoreCase));
                    tags.Add("Performer", performersString);
                }
            }
            else
            {

                tt = tag?.FirstPerformer; if (tt != null) { tags.Add("Artist", tag.FirstPerformer); }
                tt = tag?.AlbumArtists; if (tt != null) { tags.Add(nameof(tag.AlbumArtists), string.Join("|", tag.AlbumArtists)); }
                tt = tag?.AlbumArtistsSort; if (tt != null) { tags.Add(nameof(tag.AlbumArtistsSort), string.Join("|", tag.AlbumArtistsSort)); }
                tt = tag?.FirstAlbumArtist; if (tt != null) { tags.Add(nameof(tag.FirstAlbumArtist), tag.FirstAlbumArtist); }
                tt = tag?.Conductor; if (tt != null) { tags.Add(nameof(tag.Conductor), tag.Conductor); }

                //
                tt = tag?.TrackCount; if (tt != null) { tags.Add(nameof(tag.TrackCount), tag.TrackCount.ToString()); }
                tt = tag?.Disc; if (tt != null) { tags.Add(nameof(tag.Disc), tag.Disc.ToString()); }
                tt = tag?.DiscCount; if (tt != null) { tags.Add(nameof(tag.DiscCount), tag.DiscCount.ToString()); }

                //
                tt = tag?.ComposersSort; if (tt != null) { tags.Add(nameof(tag.ComposersSort), string.Join("|", tag.ComposersSort)); }
                tt = tag?.FirstComposerSort; if (tt != null) { tags.Add(nameof(tag.FirstComposerSort), tag.FirstComposerSort); }

                // musicbrainz stuff
                tt = tag?.MusicBrainzArtistId; if (tt != null) { tags.Add(nameof(tag.MusicBrainzArtistId), tag.MusicBrainzArtistId); }
                tt = tag?.MusicBrainzReleaseArtistId; if (tt != null) { tags.Add(nameof(tag.MusicBrainzReleaseArtistId), tag.MusicBrainzReleaseArtistId); }
                tt = tag?.MusicBrainzReleaseCountry; if (tt != null) { tags.Add(nameof(tag.MusicBrainzReleaseCountry), tag.MusicBrainzReleaseCountry); }
                tt = tag?.MusicBrainzReleaseId; if (tt != null) { tags.Add(nameof(tag.MusicBrainzReleaseId), tag.MusicBrainzReleaseId); }
                tt = tag?.MusicBrainzReleaseStatus; if (tt != null) { tags.Add(nameof(tag.MusicBrainzReleaseStatus), tag.MusicBrainzReleaseStatus); }
                tt = tag?.MusicBrainzReleaseType; if (tt != null) { tags.Add(nameof(tag.MusicBrainzReleaseType), tag.MusicBrainzReleaseType); }
                tt = tag?.MusicBrainzTrackId; if (tt != null) { tags.Add(nameof(tag.MusicBrainzTrackId), tag.MusicBrainzTrackId); }

                try
                {
                    tags.Add(nameof(file.Properties.AudioBitrate), file.Properties.AudioBitrate.ToString());
                    tags.Add(nameof(file.Properties.AudioSampleRate), file.Properties.AudioSampleRate.ToString());
                    tags.Add(nameof(file.Properties.Duration), file.Properties.Duration.TotalSeconds.ToString());
                }
                catch { }

                var year = 0;
                tt = tag?.Year; if (tt != null) { year = (int)tag.Year; }

                tt = tag?.Performers;
                if (tt != null)
                {
                    addPerformers(tag.Performers);
                }
                string performersString = string.Join("|", performers.Distinct(StringComparer.CurrentCultureIgnoreCase));
                tags.Add("Performer", performersString);
                var work = file.GetWork();
                if (work != null)
                {
                    tags.Add("Work", work);

                }
                var composition = file.GetComposition();
                if (composition != null)
                {
                    tags.Add("Composition", composition);
                }
                else if (work != null)
                {
                    tags.Add("Composition", work);
                }

            }

            foreach (var item in tags)
            {
                try
                {
                    if (item.Value != null && (!(item.Value is string) || ((string)item.Value != string.Empty)))
                    {
                        var idTag = new IdTag
                        {
                            MusicFile = mf,
                            Name = item.Key
                        };
                        if (idTag.Name == "Pictures")
                        {
                            Music.TagLib.IPicture p = (Music.TagLib.IPicture)item.Value;
                            idTag.PictureChecksum = p.Data.Checksum;
                            idTag.PictureMimeType = p.MimeType;
                            idTag.PictureData = p.Data.Data;
                        }
                        else
                        {
                            idTag.Value = (string)item.Value;
                        }
                        mf.IdTags.Add(idTag);
                        await musicDb.IdTags.AddAsync(idTag);

                    }

                }
                catch (Exception xe)
                {
                    log.Error(xe, $"{mf.File}, tag {item.Key} value {item.Value}");
                    throw;
                }
            }
            //return allowStyleTags;
        }
        private static string CleanName(this string name)
        {
            var parts = name.Trim().Split(new char[] { '(', '[' });
            return parts[0].Trim();
        }
        private static string Remove(string key, string multiValued)
        {
            key = $"({key})";
            var parts = multiValued.Split(new char[] { ';', ',', '|', ':' });
            return string.Join('|', parts.Where(x => !x.EndsWith(key, StringComparison.CurrentCultureIgnoreCase)));
        }
        private static string Extract(string key, string multiValued)
        {
            key = $"({key})";
            if (multiValued.Contains(key, StringComparison.CurrentCultureIgnoreCase))
            {
                var text = multiValued.Split(new char[] { ';', ',', '|', ':' }).Where(x => x.EndsWith(key, StringComparison.CurrentCultureIgnoreCase)).First();
                var result = text.Substring(0, text.IndexOf(key)).Trim();
                return result;
            }
            return null;
        }
    }
}
