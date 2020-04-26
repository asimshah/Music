using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Music.Metatools
{
    public class OpusFolder
    {
        public string Folderpath => currentPathData.GetFolderpath();
        public string Source => currentPathData.DiskRoot;
        public bool IsGenerated => currentPathData.IsGenerated;
        public bool IsCollection => currentPathData.IsCollections;// mfi.IsCollection;
        public bool HasParts => parts != null ? parts.Count() > 0 : false;
        public bool ForSinglesOnly => forSinglesOnly;
        public MusicOptions MusicOptions => mfi.MusicOptions;
        public MusicStyles MusicStyle => mfi.MusicStyle;
        public string ArtistName => currentPathData.ArtistPath;
        public string OpusName => forSinglesOnly ? "Singles" : currentPathData.OpusPath;
        private List<OpusPart> parts;
        private readonly MusicFolderInformation mfi;
        private readonly bool forSinglesOnly;
        private readonly ILogger log;
        private readonly PathData currentPathData;
        public OpusFolder(MusicFolderInformation mfi, int mfiPathsIndex, /*string path,*/ bool forSinglesOnly = false)
        {
            this.log = ApplicationLoggerFactory.CreateLogger<OpusFolder>();
            this.mfi = mfi;
            this.forSinglesOnly = forSinglesOnly;
            this.currentPathData = mfi.Paths[mfiPathsIndex];
        }
        public OpusFolder(MusicOptions musicOptions, PathData pathData)
        {
            this.log = ApplicationLoggerFactory.CreateLogger<OpusFolder>();
            this.mfi = new MusicFolderInformation
            {
                MusicOptions = musicOptions,
                MusicStyle = pathData.MusicStyle,
                Paths = new PathData[] { pathData },
                IncludeSingles = pathData.MusicStyle == MusicStyles.Popular
            };
            this.currentPathData = pathData;
            if (pathData.OpusPath == null)
            {
                this.forSinglesOnly = pathData.MusicStyle == MusicStyles.Popular;
            }
        }
        public int RemoveCurrentMusicFiles(MusicDb db, TaskItem taskItem)
        {
            //var dc = new OpusDeleteContext(this);
            var entityHelper = new EntityHelper(db, taskItem);
            int count = 0;
            var filesInDb = GetMusicFilesFromDb(db);
            foreach (var mf in filesInDb.ToArray())
            {
                ++count;
                entityHelper.Delete(mf);
                //db.Delete(mf, dc);
            }
            return count;
        }
        public async Task<List<MusicFile>> UpdateAudioFilesToDbAsync(MusicDb db)
        {
            var musicFiles = new List<MusicFile>();
            foreach (var audioFile in new AudioFileCollection(this))
            {
                var ap = audioFile.GetAudioProperties();
                var mf = await db.MusicFiles.SingleOrDefaultAsync(x => x.File == audioFile.File.FullName);
                if (mf == null)
                {

                    var opusType = IsCollection ? OpusType.Collection : OpusType.Normal;
                    if (ForSinglesOnly)
                    {
                        opusType = OpusType.Singles;
                    }
                    mf = new MusicFile
                    {
                        DiskRoot = currentPathData.DiskRoot,
                        IsGenerated = currentPathData.IsGenerated,
                        Encoding = Path.GetExtension(audioFile.File.FullName).Substring(1).To<EncodingType>(),
                        Musician = currentPathData.ArtistPath,
                        MusicianType = IsCollection ? ArtistType.Various : ArtistType.Artist,
                        OpusName = opusType == OpusType.Singles ? $"{currentPathData.ArtistPath} Singles" : currentPathData.OpusPath,
                        OpusType = opusType,
                        IsMultiPart = HasParts,
                        PartName = HasParts ? audioFile.Part.Name : string.Empty,
                        PartNumber = HasParts ? audioFile.Part.Number : 0,
                        Style = mfi.MusicStyle,
                        StylePath = currentPathData.StylePath,
                        OpusPath = IsCollection ? currentPathData.OpusPath : ForSinglesOnly ? currentPathData.ArtistPath : Path.Combine(currentPathData.ArtistPath, currentPathData.OpusPath),
                        Mode = ap.Mode,
                        File = audioFile.File.FullName,
                        Uid = Guid.NewGuid().ToString(),
                        Mood = string.Empty
                    };
                    await db.MusicFiles.AddAsync(mf);
                    log.Debug($"{mf.File} added to db");
                }
                mf.Duration = ap.Duration;
                mf.BitsPerSample = ap.BitsPerSample;
                mf.SampleRate = ap.SampleRate;
                mf.MinimumBitRate = ap.MinimumBitRate;
                mf.MaximumBitRate = ap.MaximumBitRate;
                mf.AverageBitRate = ap.AverageBitRate;
                mf.FileLastWriteTimeUtc = audioFile.File.LastWriteTimeUtc;
                mf.FileLength = audioFile.File.Length;
                mf.ParsingStage = MusicFileParsingStage.Initial;
                musicFiles.Add(mf);
            }
            return musicFiles;
        }
        //private bool HasMusicTags()
        //{
        //    var tagfile = Path.Combine(Folderpath, ITEOBase.TagFile);
        //    return File.Exists(tagfile);
        //}
        public (bool result, ChangesDetected changes) CheckForChanges(MusicDb db)
        {
            ChangesDetected changesDetected = ChangesDetected.None;
            bool result = false;
            var st = new StepTimer();
            st.Start();
            var currentMusicFiles = GetMusicFilesFromDb(db);//.ToArray();
            st.Time();
            var filesOnDisk = GetFilesOnDisk();
            st.Time();
            bool anyFilesNotCatalogued()
            {
                bool r = false;
                var list = currentMusicFiles.Where(x => x.Track == null);
                r = list.Count() > 0;
                st.Time();
                if (r)
                {
                    changesDetected = ChangesDetected.AtLeastOneFileNotCatalogued;
                    //log.Trace($"anyFilesNotCatalogued() returns true");
                }
                return r;
            }
            bool anyFilesRewritten()
            {
                bool r = false;
                var l1 = currentMusicFiles.Select(x => x.FileLastWriteTimeUtc);
                var l2 = filesOnDisk.Select(x => new DateTimeOffset(x.fi.LastWriteTimeUtc, TimeSpan.Zero));
                r = !l1.SequenceEqual(l2);
                st.Time();
                if (r)
                {
                    changesDetected = ChangesDetected.AtLeastOneFileModifiedOnDisk;
                    log.Trace($"anyFilesRewritten() returns true");
                }
                return r;
            }

            bool additionsOrDeletionsExist()
            {
                var differences = filesOnDisk.Select(f => f.fi.FullName).Except(currentMusicFiles.Select(mf => mf.File), StringComparer.CurrentCultureIgnoreCase);
                st.Time();
                var r = differences.Count() != 0;
                if (r)
                {
                    changesDetected = ChangesDetected.MusicFileCountHasChanged;
                    log.Debug($"music file difference count is {differences.Count()}");
                }
                return r;
            }
            bool anyImageChanged()
            {
                bool r = false;
                var works = currentMusicFiles.Select(mf => mf.Track).Select(x => x.Work).Distinct();
                var artists = works.SelectMany(x => x.Artists)
                    .Union(currentMusicFiles.Where(mf => mf.Track.Performance != null)
                    .Select(x => x.Track.Performance)
                    .Where(x => x.Composition != null).Select(x => x.Composition.Artist))
                    .Distinct();
                //var artistFromCompositions = performances.Where(x => x.Composition != null).Select(x => x.Composition.Artist);

                //.Select(mf => mf.Track).Where(x => x.Performance != null)

                //.Union(currentMusicFiles.Select(mf => mf.Track).Where(x => x.Performance != null)
                //.Select(x => x.Performance.Composition.Artist))
                //.Distinct();
                foreach (var artist in artists.Where(a => a.Type != ArtistType.Various))
                {
                    var f = artist.GetPortraitFile(MusicOptions);
                    if (artist.Portrait.HasChanged(f))
                    {
                        log.Debug($"artist {artist.Name}, portrait file {f} found");
                        r = true;
                        break;
                    }
                }
                if (!r)
                {
                    foreach (var work in works)
                    {
                        var coverFile = work.GetMostRecentOpusCoverFile(MusicOptions);
                        if (work.Cover.HasChanged(coverFile))
                        {
                            log.Debug($"artist(s) {work.GetArtistNames()}, work {work.Name}, cover art file {coverFile}");
                            r = true;
                            break;
                        }
                    }
                }
                if (r)
                {
                    changesDetected = ChangesDetected.CoverArtHasChanged;
                    //log.Trace($"anyImageChanged() returns true");
                }
                return r;
            }
            if (additionsOrDeletionsExist() /*|| musicTagsAreNew()*/ || anyFilesRewritten() || anyFilesNotCatalogued() || anyImageChanged())
            {
                result = true;
            }
            return (result, changesDetected);
        }
        public IEnumerable<MusicFile> GetMusicFilesFromDb(MusicDb db)
        {
            var opusPath = currentPathData.OpusPath != null ? Path.Combine(currentPathData.ArtistPath, currentPathData.OpusPath) : currentPathData.ArtistPath;
            var result = db.MusicFiles.Where(mf =>
                mf.DiskRoot.ToLower() == currentPathData.DiskRoot.ToLower()
                && mf.StylePath.ToLower() == currentPathData.StylePath.ToLower())
                .ToArray()
                .Where(mf => (mf.OpusType == OpusType.Collection ? Path.Combine("collections", mf.OpusPath.ToLower()) : mf.OpusPath.ToLower()) == opusPath.ToLower())
                ;
            return result.ToArray().OrderBy(x => x.File, StringComparer.CurrentCultureIgnoreCase);
        }
        public IEnumerable<(FileInfo fi, OpusPart part)> GetFilesOnDisk()
        {
            var list = new List<(FileInfo fi, OpusPart part)>();
            var path = Folderpath;
            if (!Directory.Exists(path))
            {
                path = path.RemoveDiacritics();
            }
            var parts = forSinglesOnly ? null : GetParts(path);
            if (parts != null && parts.Count() > 0)
            {
                foreach (var part in parts)
                {
                    var combinedPath = Path.Combine(path, part.Name);
                    list.AddRange(mfi.MusicOptions.GetMusicFiles(combinedPath).Select(f => (f, part)));
                }
            }
            else
            {
                list.AddRange(mfi.MusicOptions.GetMusicFiles(path).Select<FileInfo, (FileInfo, OpusPart)>(f => (f, null)));
            }
            return list.OrderBy(x => x.fi.FullName, StringComparer.CurrentCultureIgnoreCase);
        }
        private IEnumerable<OpusPart> GetParts(string path)
        {
            if (parts == null)
            {
                parts = new List<OpusPart>();
                int partNumber = 0;
                foreach (var subdirectory in Directory.EnumerateDirectories(path, "*.*", SearchOption.TopDirectoryOnly).OrderBy(x => x, new NaturalStringComparer()))
                {
                    var containsMusic = Directory.EnumerateFiles(subdirectory, "*.*").Any(f => mfi.MusicOptions.MusicFileExtensions.Contains(Path.GetExtension(f), StringComparer.CurrentCultureIgnoreCase));
                    if (containsMusic)
                    {
                        var name = Path.GetFileName(subdirectory);
                        parts.Add(new OpusPart { Name = name, Number = partNumber++ });
                    }
                }
            }
            return parts;
        }
        public override string ToString()
        {
            var relativeFolder = Path.GetRelativePath(currentPathData.GetFullArtistPath().RemoveDiacritics(), Folderpath.RemoveDiacritics());
            if (ForSinglesOnly)
            {
                return $"[{currentPathData.DiskRoot}][{currentPathData.ArtistPath}]: Singles";
            }
            else
            {
                return $"[{currentPathData.DiskRoot}][{currentPathData.ArtistPath}]: {relativeFolder}";
            }
        }
        public string ToContextDescription()
        {
            //{(this.ArtistPath ?? "null")}::{(this.OpusPath ?? "null")}
            return $"{(this.currentPathData.ArtistPath ?? "null")}::{(this.currentPathData.OpusPath ?? "null")}";
        }
        private void AssertMatch(MusicFile mf, AudioFile audioFile, bool checkAudioProperties = false)
        {
            var pd = currentPathData;// this.mfi.Paths[this.mfiPathsIndex];
            var opusType = IsCollection ? OpusType.Collection : OpusType.Normal;
            if (ForSinglesOnly)
            {
                opusType = OpusType.Singles;
            }

            Debug.Assert(string.Compare(mf.File, audioFile.File.FullName, true) == 0);
            Debug.Assert(mf.DiskRoot.IsEqualIgnoreAccentsAndCase(pd.DiskRoot));
            Debug.Assert(mf.IsGenerated == pd.IsGenerated);
            Debug.Assert(mf.Encoding == Path.GetExtension(audioFile.File.FullName).Substring(1).To<EncodingType>());
            Debug.Assert(string.Compare(mf.Musician, pd.ArtistPath, true) == 0);
            Debug.Assert(mf.MusicianType == (IsCollection ? ArtistType.Various : ArtistType.Artist));
            Debug.Assert(mf.OpusName == pd.OpusPath);
            //temp: Debug.Assert(mf.OpusType == opusType);
            Debug.Assert(mf.IsMultiPart == HasParts);
            Debug.Assert(mf.PartName == (HasParts ? audioFile.Part.Name : string.Empty));
            Debug.Assert(mf.PartNumber == (HasParts ? audioFile.Part.Number : 0));
            Debug.Assert(mf.Style == MusicStyle);
            Debug.Assert(mf.StylePath == pd.StylePath);
            switch (opusType)
            {
                case OpusType.Collection:
                    Debug.Assert(mf.OpusPath == pd.OpusPath);
                    break;
                case OpusType.Normal:
                    Debug.Assert(pd.OpusPath != null);
                    Debug.Assert(mf.OpusPath == Path.Combine(pd.ArtistPath, pd.OpusPath));
                    break;
                case OpusType.Singles:
                    Debug.Assert(mf.OpusPath == pd.ArtistPath);
                    break;
                default:
                    //Debugger.Break();
                    break;
            }

            if (checkAudioProperties)
            {
                var ap = audioFile.GetAudioProperties();
                Debug.Assert(mf.Mode == ap.Mode);
                Debug.Assert(mf.Duration == ap.Duration);
                Debug.Assert(mf.BitsPerSample == ap.BitsPerSample);
                Debug.Assert(mf.SampleRate == ap.SampleRate);
                Debug.Assert(mf.MinimumBitRate == ap.MinimumBitRate);
                Debug.Assert(mf.MaximumBitRate == ap.MaximumBitRate);
                Debug.Assert(mf.AverageBitRate == ap.AverageBitRate);
            }
        }
    }
    public enum ChangesDetected
    {
        None,
        AtLeastOneFileNotCatalogued,
        AtLeastOneFileModifiedOnDisk,
        //MusicTagsAreNewer,
        MusicFileCountHasChanged,
        CoverArtHasChanged
    }

}
