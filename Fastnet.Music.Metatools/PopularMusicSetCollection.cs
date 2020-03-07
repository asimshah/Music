using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using System.Collections.Generic;
using System.Linq;

namespace Fastnet.Music.Metatools
{
    /// <summary>
    /// Popular music files are grouped in one of 3 ways: (1) if the MusicFolder is an ArtistFolder then music files in this folder are
    /// grouped into a 'singles' album; (2) if it is a Collection fodler then music files are grouped by artist into 'singles' folders;
    /// and (3) if it is an opus folder then all music files in the folder are collected into an album
    /// </summary>
    public class PopularMusicSetCollection : MusicSetCollection<PopularMusicTags>
    {
        public PopularMusicSetCollection(MusicOptions musicOptions, MusicDb musicDb,
            OpusFolder musicFolder, List<MusicFile> files, TaskItem taskItem) : base(musicOptions, musicDb, musicFolder, files, taskItem)
        {
            this.log = ApplicationLoggerFactory.CreateLogger<PopularMusicSetCollection>();
        }
        protected override List<IMusicSet> CreateSets()
        {
            var result = new List<IMusicSet>();
            var artistName = musicOptions.ReplaceAlias(musicFolder.ArtistName);
            var albumName = string.Empty;
            switch (musicFolder)
            {
                case OpusFolder opusFolder:
                    albumName = opusFolder.OpusName;
                    if (opusFolder.IsCollection)
                    {
                        string getArtistName(MusicFile mf)
                        {
                            return musicOptions.ReplaceAlias(mf.GetArtistName());
                        }
                        var artistGroups = files.Select(f => new { file = f, artist = getArtistName(f), work = $"{getArtistName(f)} Singles" })
                            .GroupBy(gb => new { gb.artist, gb.work });
                        foreach (var group in artistGroups)
                        {
                            var musicFilesForSet = group.Select(g => g.file).OrderBy(f => f.GetTagIntValue("TrackNumber"));
                            var set = new PopularMusicAlbumSet(musicDb, this.musicOptions, group.Key.artist, group.Key.work, musicFilesForSet, taskItem);
                            result.Add(set);
                        }
                    }
                    else
                    {
                        var set2 = new PopularMusicAlbumSet(musicDb, this.musicOptions, artistName, albumName, files, taskItem);
                        result.Add(set2);
                    }
                    break;
            }
            return result;
        }
    }
}
