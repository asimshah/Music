namespace Fastnet.Music.Metatools
{
    /// <summary>
    /// base class for Apollo music scheme folders
    /// (what is the Name property for?)
    /// </summary>
    //public abstract class MusicFolder : BaseMusicMetaDataOld
    //{
    //    public string Name { get; private set; }
    //    public bool IsSingles { get; protected set; }
    //    public bool IsCollection { get; protected set; }
    //    /// <summary>
    //    /// OpusFolder only have one item in this list, ArtistFolders can have multiple items
    //    /// </summary>
    //    public List<PathData> PathDataList { get; set; } = new List<PathData>();
    //    public bool MusicIsGenerated { get; protected set; }//{ get; set; }
    //    public List<OpusPart> Parts { get; set; } = new List<OpusPart>();
    //    public MusicFolder(MusicOptions musicOptions, string name) : base(musicOptions)
    //    {
    //        this.Name = name;
    //    }
    //    public MusicFolder(MusicOptions musicOptions, MusicStyles musicStyle, string name) : base(musicOptions, musicStyle)
    //    {
    //        this.Name = name;
    //    }
    //    internal abstract bool HasCustomTags();
    //    internal abstract IEnumerable<MusicFile> GetMusicFilesFromDb(MusicDb db);
    //    internal abstract IEnumerable<FileInfo> GetFilesOnDisk();
    //    //public async Task<List<MusicFile>> UpdateAudioFilesToDbAsync(MusicDb db)
    //    //{
    //    //    var pathdata = PathDataList.First();
    //    //    var musicFiles = new List<MusicFile>();
    //    //    foreach (var audioFile in MusicFolderTools.GetAudioFiles(this))
    //    //    {
    //    //        if (this is ArtistFolderOld)
    //    //        {
    //    //            // find match pathdata
    //    //            pathdata = PathDataList.Single(x => audioFile.File.FullName.StartsWithIgnoreAccentsAndCase(x.GetFullArtistPath()));

    //    //        }
    //    //        var mf = await db.MusicFiles.SingleOrDefaultAsync(x => x.File == audioFile.File.FullName);
    //    //        if (mf != null)
    //    //        {
    //    //            AssertMatch(pathdata, mf, audioFile);
    //    //        }
    //    //        else
    //    //        {
    //    //            var ap = audioFile.GetAudioProperties();
    //    //            var opusType = audioFile.IsSingle ? OpusType.Singles : (IsCollection ? OpusType.Collection : OpusType.Normal);
    //    //            mf = new MusicFile
    //    //            {
    //    //                DiskRoot = pathdata.DiskRoot,
    //    //                IsGenerated = MusicIsGenerated,
    //    //                Encoding = Path.GetExtension(audioFile.File.FullName).Substring(1).To<EncodingType>(),
    //    //                Musician = pathdata.ArtistPath,
    //    //                MusicianType = IsCollection ? ArtistType.Various : ArtistType.Artist,
    //    //                OpusName = opusType == OpusType.Singles ? $"{pathdata.ArtistPath} Singles" : pathdata.OpusPath,
    //    //                OpusType = opusType,
    //    //                IsMultiPart = HasParts(),
    //    //                PartName = HasParts() ? audioFile.Part.Name : string.Empty,
    //    //                PartNumber = HasParts() ? audioFile.Part.Number : 0,
    //    //                Style = musicStyle,
    //    //                StylePath = pathdata.StylePath,
    //    //                OpusPath = IsCollection ? pathdata.OpusPath : IsSingles ? pathdata.ArtistPath : Path.Combine(pathdata.ArtistPath, pathdata.OpusPath),
    //    //                Mode = ap.Mode,
    //    //                Duration = ap.Duration,
    //    //                BitsPerSample = ap.BitsPerSample,
    //    //                SampleRate = ap.SampleRate,
    //    //                MinimumBitRate = ap.MinimumBitRate,
    //    //                MaximumBitRate = ap.MaximumBitRate,
    //    //                AverageBitRate = ap.AverageBitRate,
    //    //                File = audioFile.File.FullName,
    //    //                FileLastWriteTimeUtc = audioFile.File.LastWriteTimeUtc,
    //    //                FileLength = audioFile.File.Length,
    //    //                Uid = Guid.NewGuid().ToString(),
    //    //                ParsingStage = MusicFileParsingStage.Initial,
    //    //                Mood = string.Empty
    //    //            };
    //    //            await db.MusicFiles.AddAsync(mf);
    //    //            log.Debug($"{mf.File} added to db");
    //    //            //log.Information($"file {audioFile.File.FullName} not found in db");
    //    //        }
    //    //        musicFiles.Add(mf);
    //    //    }
    //    //    return musicFiles;
    //    //}
    //    // returns true if there are changes that require re-cataloguing
    //    public abstract bool CheckForChanges(MusicDb db);
    //    /// <summary>
    //    /// removes music files and related catalogue entries from the database
    //    /// </summary>
    //    /// <param name="db"></param>
    //    public int RemoveCurrentMusicFiles(MusicDb db)
    //    {
    //        int count = 0;
    //        var filesInDb = GetMusicFilesFromDb(db);
    //        foreach (var mf in filesInDb.ToArray())
    //        {
    //            ++count;
    //            db.Delete(mf);
    //        }
    //        return count;
    //    }
    //    internal bool HasParts()
    //    {
    //        return Parts.Count > 0;
    //    }
    //    private void AssertMatch(PathData pd, MusicFile mf, AudioFile audioFile, bool checkAudioProperties = false)
    //    {
    //        var opusType = audioFile.IsSingle ? OpusType.Singles : (IsCollection ? OpusType.Collection : OpusType.Normal);

    //        Debug.Assert(string.Compare(mf.File, audioFile.File.FullName, true) == 0);
    //        Debug.Assert(mf.DiskRoot.IsEqualIgnoreAccentsAndCase(pd.DiskRoot));
    //        Debug.Assert(mf.IsGenerated == MusicIsGenerated);
    //        Debug.Assert(mf.Encoding == Path.GetExtension(audioFile.File.FullName).Substring(1).To<EncodingType>());
    //        Debug.Assert(mf.Musician == pd.ArtistPath);
    //        Debug.Assert(mf.MusicianType == (IsCollection ? ArtistType.Various : ArtistType.Artist));
    //        Debug.Assert(mf.OpusName == pd.OpusPath);
    //        Debug.Assert(mf.OpusType == opusType);
    //        Debug.Assert(mf.IsMultiPart == HasParts());
    //        Debug.Assert(mf.PartName == (HasParts() ? audioFile.Part.Name : string.Empty));
    //        Debug.Assert(mf.PartNumber == (HasParts() ? audioFile.Part.Number : 0));
    //        Debug.Assert(mf.Style == musicStyle);
    //        Debug.Assert(mf.StylePath == pd.StylePath);
    //        Debug.Assert(mf.OpusPath == (IsCollection ? pd.OpusPath : Path.Combine(pd.ArtistPath, pd.OpusPath)));

    //        if (checkAudioProperties)
    //        {
    //            var ap = audioFile.GetAudioProperties();
    //            Debug.Assert(mf.Mode == ap.Mode);
    //            Debug.Assert(mf.Duration == ap.Duration);
    //            Debug.Assert(mf.BitsPerSample == ap.BitsPerSample);
    //            Debug.Assert(mf.SampleRate == ap.SampleRate);
    //            Debug.Assert(mf.MinimumBitRate == ap.MinimumBitRate);
    //            Debug.Assert(mf.MaximumBitRate == ap.MaximumBitRate);
    //            Debug.Assert(mf.AverageBitRate == ap.AverageBitRate);
    //        }
    //    }
    //    public override string ToString()
    //    {
    //        return string.Join(", ", PathDataList);
    //    }
    //}
}
