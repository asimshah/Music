using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Metatools;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Music.Metatools
{
    public class WesternClassicalAlbumTEO : TEOBase // TEOBase<WesternClassicalMusicFileTEO
    {
        public WesternClassicalAlbumTEO(MusicOptions musicOptions) : base(musicOptions)
        {
        }

        /// <summary>
        /// list of TEO for each performance in this album
        /// </summary>
        public IEnumerable<PerformanceTEO> PerformanceList { get; set; }

        public override void SaveChanges(Work work)
        {
            base.SaveChanges(work);
            //var performancesInDb = work.Tracks.Select(x => x.Performance);
            foreach(var performance in PerformanceList)
            {
                performance.RecordChanges();
            }
        }
        protected override void LoadTags()
        {
            //now group music files and load performances
            var groupedByComposition = TrackList.Cast<WesternClassicalMusicFileTEO>().GroupBy(k => k.CompositionTag.Values.First().Value);
            var result = new List<PerformanceTEO>();
            var tagValueComparer = new TagValueComparer();
            foreach (var group in groupedByComposition.OrderBy(x => x.Key))
            {
                var movementFilenames = group
                    .Select(x => x.File);
                var teo = new PerformanceTEO(musicOptions);
                
                teo.LoadTags(movementFilenames, TrackList.Cast<WesternClassicalMusicFileTEO>());
                result.Add(teo);
            }
            PerformanceList = result;
        }
        public override void AfterDeserialisation(Work work)
        {
            base.AfterDeserialisation(work);
            foreach (var p in PerformanceList)
            {
                p.SetMovementTEOList(TrackList.Cast<WesternClassicalMusicFileTEO>());
            }
        }

        protected override MusicFileTEO CreateMusicFileTeo(MusicFile mf)
        {
            return new WesternClassicalMusicFileTEO(musicOptions) { MusicFile = mf };
        }
        //public async override Task Load(Work work)
        //{
        //    await base.Load(work);
        //    TrackList
        //}
    }
}
