using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Metatools;
using Newtonsoft.Json;
using System;
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

        public override void SaveChanges(MusicDb db, Work work)
        {
            base.SaveChanges(db, work);
            //var performancesInDb = work.Tracks.Select(x => x.Performance);
            foreach(var performance in PerformanceList)
            {
                performance.RecordChanges(db);
            }

        }
        protected override void LoadTags()
        {
            //now group music files and load performances
            var groupedByComposition = TrackList/*.Cast<WesternClassicalMusicFileTEO>()*/.GroupBy(k => k.CompositionTag.Values.First().Value);
            var result = new List<PerformanceTEO>();
            var tagValueComparer = new TagValueComparer();
            foreach (var group in groupedByComposition.OrderBy(x => x.Key))
            {
                //if(group.Key.Contains("Alla"))
                //{
                //    Debugger.Break();
                //}
                var movementFilenames = group
                    .Select(x => x.File);
                var teo = new PerformanceTEO(musicOptions);
                
                teo.LoadTags(movementFilenames, TrackList/*.Cast<WesternClassicalMusicFileTEO>()*/);
                result.Add(teo);
            }
            PerformanceList = result;
        }
        public override void AfterDeserialisation(Work work)
        {
            base.AfterDeserialisation(work);
            foreach (var p in PerformanceList)
            {
                p.SetMovementTEOList(TrackList/*.Cast<WesternClassicalMusicFileTEO>()*/);
            }
        }

        protected override MusicFileTEO CreateMusicFileTeo(MusicFile mf)
        {
            //return new WesternClassicalMusicFileTEO(musicOptions) { MusicFile = mf };
            return new MusicFileTEO(musicOptions) { MusicFile = mf };
        }
    }
    //public class WesternClassicalAlbumTEOConverter : JsonConverter
    //{
    //    public override bool CanConvert(Type objectType)
    //    {
    //        var r =  typeof(WesternClassicalAlbumTEO).IsAssignableFrom(objectType);
    //        return r;
    //    }

    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
