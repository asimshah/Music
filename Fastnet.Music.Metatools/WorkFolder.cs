using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Music.Metatools
{
    /// <summary>
    /// a work folder is a disk folder (except for "singles") for
    /// (1) popular:
    ///     - an album, parent is an ArtistFolder
    ///     - the "Singles" 'album' (contains individual music files found in the ArtistFolder)
    /// (2) western classical:
    ///     - if parent is an ArtistFolder then a composition - there may be many folders for the same composition but each has a unique folder name
    ///     - if parent is a collection, then an album (or other named set of music files) with multiple artists and compositions
    /// (3) indian classical:
    ///     - parent is always ArtistFolder, an album (or other named set of music files)
    /// (4) hindi films:
    ///     - if this is a topfolder, then a film
    ///     - if the parent is a collection, then an album (or other named set of music files) with multiple artists and films
    /// </summary>
    public abstract class WorkFolder : ITopFolder
    {
        private List<OpusPart> parts;
        public string Fullpath => GetFullPath();
        public MusicRoot MusicRoot => musicRoot;
        public AlbumType Type => type;
        public MusicStyles MusicStyle => musicRoot.MusicStyle;
        public bool HasParts => parts != null ? parts.Count() > 0 : false;
        protected readonly MusicRoot musicRoot;
        private readonly AlbumType type = AlbumType.Normal;
        protected readonly string workName;
        protected readonly string artistName; // null if work is a collection
        public WorkFolder(MusicRoot mr, string artistName, string workName, AlbumType type = AlbumType.Normal)
        {
            musicRoot = mr;
            this.workName = workName;
            this.artistName = artistName;
            this.workName = workName;
            this.type = type;
        }
        public bool HasMusicFiles()
        {
            return GetMusicFilePaths().Any(x => ContainsMusic(x.path));
        }
        public IEnumerable<AudioFile> GetAudioFiles()
        {
            return new AudioFileCollection(this);
        }
        public IEnumerable<(FileInfo fi, OpusPart part)> GetFilesOnDisk()
        {
            var list = new List<(FileInfo fi, OpusPart part)>();
            var paths = GetMusicFilePaths();
            return paths.SelectMany(p => GetMusicFiles(p.path).Select(f => (fi: f, p.part)))
                .OrderBy(x => x.part?.Number ?? 0)
                .ThenBy(x => x.fi.FullName, StringComparer.CurrentCultureIgnoreCase);
        }
        /// <summary>
        /// Finds music files in the db that match the path for this folder
        /// The path  for this folder can be overridden using the 'pathToUse' parameter (useful for deleted paths)
        /// </summary>
        /// <param name="eh"></param>
        /// <param name="pathToUse">use this path in place of the normal path</param>
        /// <returns></returns>
        public IEnumerable<MusicFile> GetFilesInDb(EntityHelper eh, string pathToUse = null)
        {
            var paths = pathToUse == null ? GetMusicFilePaths().Select(x => x.path) : new string[] { pathToUse };
            IEnumerable<MusicFile> result = Enumerable.Empty<MusicFile>();
            foreach (var path in paths)
            {
                result = result.Union(eh.FindMatchingFiles(path));
            }
            return result.ToArray();
        }
        public async Task<MusicFile> AddMusicFile(EntityHelper eh, AudioFile audioFile)
        {
            var ap = audioFile.GetAudioProperties();
            var mf = await eh.FindMusicFileAsync(audioFile.File.FullName);// db.MusicFiles.SingleOrDefaultAsync(x => x.File == audioFile.File.FullName);
            if (mf == null)
            {
                var opusType = Type switch
                {
                    AlbumType.Singles => OpusType.Singles,
                    AlbumType.Collection => OpusType.Collection,
                    _ => OpusType.Normal
                };
                mf = new MusicFile
                {
                    DiskRoot = MusicRoot.DiskRoot,
                    IsGenerated = MusicRoot.IsGenerated,
                    Encoding = Path.GetExtension(audioFile.File.FullName).Substring(1).To<EncodingType>(),
                    Musician = string.Empty,
                    MusicianType = opusType == OpusType.Collection ? ArtistType.Various : ArtistType.Artist,
                    //OpusName = opusType == OpusType.Singles ? $"{currentPathData.ArtistPath} Singles" : currentPathData.OpusPath,
                    OpusType = opusType,
                    IsMultiPart = HasParts,
                    PartName = HasParts ? audioFile.Part.Name : string.Empty,
                    PartNumber = HasParts ? audioFile.Part.Number : 0,
                    Style = MusicRoot.MusicStyle,
                    StylePath = MusicRoot.StylePathFragment,
                    //OpusPath = Path.GetRelativePath(),// IsCollection ? currentPathData.OpusPath : ForSinglesOnly ? currentPathData.ArtistPath : Path.Combine(currentPathData.ArtistPath, currentPathData.OpusPath),
                    Mode = ap.Mode,
                    File = audioFile.File.FullName,
                    Uid = Guid.NewGuid().ToString(),
                    Mood = string.Empty
                };

                switch (this)
                {
                    case HindiFilmFolder hf:
                        mf.OpusName = hf.FilmName;
                        mf.OpusPath = hf.FilmName;
                        break;
                    case AlbumFolder af:
                        if (af.Type != AlbumType.Collection)
                        {
                            mf.Musician = af.ArtistName;
                        }
                        mf.OpusName = af.Type == AlbumType.Singles ? $"{af.ArtistName} {af.AlbumName}" : af.AlbumName;
                        mf.OpusPath = af.Type == AlbumType.Collection ? af.AlbumName
                            : af.Type == AlbumType.Singles ? af.ArtistName : Path.Combine(af.ArtistName, af.AlbumName);
                        break;
                }
                await eh.AddEntityAsync(mf);
                //await db.MusicFiles.AddAsync(mf);
                //log.Debug($"{mf.File} added to db");
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
            return mf;
        }
        /// <summary>
        /// returns either (1) the full path for this work folder, or (2) a list of paths for each part if the work folder has multiple parts
        /// </summary>
        /// <returns></returns>
        private IEnumerable<(string path, OpusPart part)> GetMusicFilePaths()
        {
            var path = GetFullPath();
            if (!Directory.Exists(path))
            {
                path = path.RemoveDiacritics();
            }
            var parts = type == AlbumType.Singles ? null : GetParts(path);
            if (parts != null && parts.Count() > 0)
            {
                return parts.Select(p => (Path.Combine(path, p.Name), p));
            }
            else
            {
                return new (string path, OpusPart part)[] { (path, null) };
            }
        }
        private IEnumerable<OpusPart> GetParts(string path)
        {
            if (parts == null)
            {
                parts = new List<OpusPart>();
                int partNumber = 0;
                if (Directory.Exists(path))
                {
                    foreach (var subdirectory in Directory.EnumerateDirectories(path, "*.*", SearchOption.TopDirectoryOnly).OrderBy(x => x, new NaturalStringComparer()))
                    {
                        //var containsMusic = Directory.EnumerateFiles(subdirectory, "*.*").Any(f => _musicFileExtensions.Contains(Path.GetExtension(f), StringComparer.CurrentCultureIgnoreCase));
                        if (ContainsMusic(subdirectory))
                        {
                            var name = Path.GetFileName(subdirectory);
                            parts.Add(new OpusPart { Name = name, Number = partNumber++ });
                        }
                    }
                }
            }
            return parts;
        }
        private bool ContainsMusic(string path)
        {
            return Directory.EnumerateFiles(path, "*.*").Any(f => StringConstants.MusicFileExtensions.Contains(Path.GetExtension(f), StringComparer.CurrentCultureIgnoreCase));
        }
        private string GetFullPath()
        {
            string fp = null;
            switch (this.type)
            {
                case AlbumType.Normal:
                    fp = Path.Combine(musicRoot.GetPath(), this.artistName, this.workName);
                    break;
                case AlbumType.Collection:
                    fp = Path.Combine(musicRoot.GetPath(), StringConstants.Collections, this.workName);
                    break;
                case AlbumType.Singles:
                    fp = Path.Combine(musicRoot.GetPath(), this.artistName);
                    break;
            }
            return fp;
        }
        private IEnumerable<FileInfo> GetMusicFiles(string srcPath, bool deep = false)
        {
            List<FileInfo> list = new List<FileInfo>();
            foreach (var ext in StringConstants.MusicFileExtensions)
            {
                var files = Directory.EnumerateFiles(srcPath, "*" + ext, deep ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                        //.AsParallel()
                        .Select(x => new FileInfo(x));
                list.AddRange(files);
            }
            return list.OrderBy(fi => fi.Name);
        }
    }
}