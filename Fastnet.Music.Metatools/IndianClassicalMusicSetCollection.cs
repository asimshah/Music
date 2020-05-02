using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Fastnet.Music.Metatools
{
    public class IndianClassicalMusicSetCollection : BaseMusicSetCollection<IndianClassicalAlbumSet, IndianClassicalRagaSet> //<IndianClassicalMusicTags>
    {
        public IndianClassicalMusicSetCollection(MusicOptions musicOptions, MusicDb musicDb, OpusFolder musicFolder, List<MusicFile> files, TaskItem taskItem) : base(musicOptions, musicDb, musicFolder, files, taskItem)
        {
            //this.log = ApplicationLoggerFactory.CreateLogger<IndianClassicalMusicSetCollection>();
        }
        protected override (string firstLevel, string secondLevel) GetPartitioningKeys(MusicFile mf)
        {
            var artist = mf.GetAllPerformers(musicOptions)
                .Where(x => x.Type == PerformerType.Artist)
                .Select(x => x.Name.ToAlphaNumerics()).ToCSV();
            var raga = mf.GetRagaName();
            return (artist, raga);
        }
        protected override (string firstLevel, string secondLevel) GetKeysForCollectionPartitioning(MusicFile mf)
        {
            return ("Various Artists", mf.OpusName);
        }
        //protected override List<BaseMusicSet> CreateSets()
        //{
        //    var result = new List<BaseMusicSet>();
        //    // first create the album set - this will create all the tracks
        //    // from which movements will be selected as required later on
        //    var albumSet = new IndianClassicalAlbumSet(musicDb, musicOptions, files, taskItem);
        //    result.Add(albumSet);
        //    if (!(files.First().IsGenerated))
        //    {
        //        (string groupKey, IEnumerable<MetaPerformer> artists, IEnumerable<MetaPerformer> remainder) getKeyAndPerformers(MusicFile mf)
        //        {
        //            var allPerformers = mf.GetAllPerformers(musicOptions);
        //            var artists = allPerformers.Where(x => x.Type == PerformerType.Artist);
        //            var remainder = allPerformers.Where(x => x.Type != PerformerType.Artist);
        //            var artistsKey = string.Join(string.Empty, artists.Select(x => x.Name.ToAlphaNumerics()));
        //            return (artistsKey, artists, remainder);
        //        }
        //        static string getRagaName(MusicFile mf)
        //        {
        //            return mf.GetRagaName();
        //        }
        //        var groups = files.Select(f => new { file = f, raga = getRagaName(f), kp = getKeyAndPerformers(f) })
        //                .GroupBy(gb => new {names = gb.kp.groupKey, ragaName = gb.raga });
        //        foreach (var group in groups.OrderBy(k => k.Key.names))
        //        {
        //            //var ragaName = group.Key.ragaName;
        //            //var artists = group.SelectMany(g => g.kp.artists).Distinct();
        //            //var otherPerformers = group.SelectMany(g => g.kp.remainder).Distinct();
        //            var musicFilesForSet = group.Select(g => g.file).OrderBy(f => f.GetTagIntValue("TrackNumber"));
        //            Debug.Assert(musicFilesForSet.Count() > 0);
        //            var set = new IndianClassicalRagaSet(musicDb, musicOptions, /*ragaName, artists, otherPerformers,*/ musicFilesForSet, taskItem);
        //            result.Add(set);
        //        }
        //    }
        //    return result;
        //}
    }
}
