using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Fastnet.Music.Metatools
{
    /// <summary>
    /// Western classsical music files are grouped by composition within artist
    /// </summary>
    public class WesternClassicalMusicSetCollection : MusicSetCollection<WesternClassicalMusicTags>
    {
        public WesternClassicalMusicSetCollection(MusicOptions musicOptions, MusicDb musicDb,
            OpusFolder musicFolder, List<MusicFile> files, TaskItem taskItem) : base(musicOptions, musicDb, musicFolder, files, taskItem)
        {
            this.log = ApplicationLoggerFactory.CreateLogger<WesternClassicalMusicSetCollection>();
        }
        protected override List<IMusicSet> CreateSets()
        {
            var result = new List<IMusicSet>();
            // first create a western classical album set
            var wcalbumSet = new WesternClassicalAlbumSet(musicDb, musicOptions, files, taskItem);
            result.Add(wcalbumSet);
            if (!(files.First().IsGenerated))
            {
                var artistAndWorkGroups = files.Select(f => new { file = f, artist = f.GetArtistName(), alphaMericWorkname = f.GetWorkName().ToAlphaNumerics(),  work = f.GetWorkName() })
                    .GroupBy(gb => new { gb.artist, gb.alphaMericWorkname });
                foreach (var group in artistAndWorkGroups.OrderBy(k => k.Key.artist))
                {
                    var musicStyle = group.First().file.Style;
                    var musicFilesForSet = group.Select(g => g.file).OrderBy(f => f.GetTagIntValue("TrackNumber"));
                    Debug.Assert(musicFilesForSet.Count() > 0);
                    var setWC = new WesternClassicalCompositionSet(musicDb, musicOptions, group.Key.artist, group.First().work, musicFilesForSet, this.taskItem);
                    result.Add(setWC);
                }
            }
            return result;
        }
    }
}
