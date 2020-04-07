using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Metatools;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Music.Metatools
{
    public interface ITEOBase
    {
        //public const string TagFile = "$TagFile.json";
        long Id { get; }
        string PathToMusicFiles { get; }
        public MusicFileTEO[] TrackList { get; }
        void AfterDeserialisation(Work work);
        void SaveChanges(MusicDb db, Work work);
        //Task SaveMusicTags();
    }
    public abstract class TEOBase : ITEOBase //where MFTEO : MusicFileTEO, new()/* where CFT : MusicTags*/
    {

        protected TagValueComparer tagValueComparer = new TagValueComparer();
        protected readonly ILogger log;
        protected readonly MusicOptions musicOptions;
        public TEOBase(MusicOptions musicOptions)
        {
            this.musicOptions = musicOptions;
            log = ApplicationLoggerFactory.CreateLogger(this.GetType());
        }
        [JsonProperty]
        public long Id { get; private set; }
        public string PathToMusicFiles { get; set; }
        [JsonProperty]
        protected TagValueStatus ArtistTag { get; set; }
        /// <summary>
        /// Value of ALBUM tag, valid and in SingleValue if common to all music files
        /// else false and multiple values in MultipleValues
        /// </summary>
        public TagValueStatus AlbumTag { get; set; }
        /// <summary>
        /// Value of YEAR tag, valid and in SingleValue if common to all music files
        /// else false and multiple values in MultipleValues
        /// </summary>
        public TagValueStatus YearTag { get; set; }
        /// <summary>
        /// list of TEO for each track (using best quality)
        /// </summary>
        public MusicFileTEO[] TrackList { get; set; }
        public bool TrackNumbersValid { get; set; }
        MusicFileTEO[] ITEOBase.TrackList { get => TrackList;  }

        //public async Task SaveMusicTags()
        //{
        //    var filename = Path.Combine(PathToMusicFiles, ITEOBase.TagFile);
        //    var text = this.ToJson(true);//, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
        //    await System.IO.File.WriteAllTextAsync(filename, text);

        //}
        //[Obsolete]
        //public async Task<string> TestTags()
        //{
        //    var filename = Path.Combine(PathToMusicFiles, ITEOBase.TagFile);
        //    var jsonText = await File.ReadAllTextAsync(filename);
        //    return jsonText;
        //}
        protected abstract MusicFileTEO CreateMusicFileTeo(MusicFile mf);
        public virtual async Task Load( Work work)
        {
            await Task.Delay(0);
            Id = work.Id;
            var files = work.Tracks
                .Select(x => (trackNumber: x.Number, musicFile: x.GetBestQuality()));
            TrackList = files
                .Select(f => CreateMusicFileTeo(f.musicFile))
                .ToArray();
            foreach(var trackteo in TrackList)
            {
                trackteo.ResetMetadata();
            }
            TrackList = TrackList.OrderBy(x => x.MusicFile.PartNumber).ThenBy(x => x.TrackNumberTag.GetValue<int>()).ToArray();
            //var filePaths = TrackList.Select(f =>
            //    Path.Combine(f.MusicFile.DiskRoot, f.MusicFile.StylePath, f.MusicFile.OpusPath))
            //    .Distinct(StringComparer.CurrentCultureIgnoreCase);
            var filePaths = TrackList.Select(f => f.MusicFile.GetRootPath())
                .Distinct(StringComparer.CurrentCultureIgnoreCase);
            Debug.Assert(filePaths.Count() == 1);
            PathToMusicFiles = filePaths.First();
            //var filename = Path.Combine(PathToMusicFiles, ITEOBase.TagFile);
            //if (File.Exists(filename))
            //{
            //    var jsonText = await File.ReadAllTextAsync(filename);
            //    JsonConvert.PopulateObject(jsonText, this);
            //    Id = work.Id; // put id back as it may have changed since the tagfile was created
            //    AfterDeserialisation(work);
            //}
            //else
            //{
            //    LoadCommonMetadata();
            //    LoadTags();
            //}
            LoadCommonMetadata();
            LoadTags();
        }
        public virtual void SaveChanges(MusicDb db,Work work)
        {
            //if (work.Artist.Type != ArtistType.Various && !ArtistTag.GetValue<string>().IsEqualIgnoreAccentsAndCase(work.Artist.Name))
            //{
            //    log.Information($"{work.Artist.Name}, \"{work.Name}\": Artist changed from {work.Artist.Name} to {ArtistTag.GetValue<string>()}");
            //    log.Warning("Artist name changes are not supported");
            //}
            if (AlbumTag.GetValue<string>() != work.Name)
            {
                log.Information($"{work.GetArtistNames()}, \"{work.Name}\": Album changed from {work.Name} to {AlbumTag.GetValue<string>()}");
                work.Name = AlbumTag.GetValue<string>();
            }
            if (YearTag.GetValue<int>() != work.Year)
            {
                log.Information($"{work.GetArtistNames()}, \"{work.Name}\": Year changed from {work.Name} to {AlbumTag.GetValue<string>()}");
                work.Year = YearTag.GetValue<int>();
            }
            foreach(var mfteo in TrackList)
            {
                var track = mfteo.MusicFile.Track;
                if(mfteo.TrackNumberTag.GetValue<int>() != track.Number)
                {
                    log.Information($"{work.GetArtistNames()}, \"{work.Name}\": track number {track.Number} changed to {mfteo.TrackNumberTag.GetValue<int>()}");
                    track.Number = mfteo.TrackNumberTag.GetValue<int>();
                }
                if (mfteo.TitleTag.GetValue<string>() != track.Title)
                {
                    log.Information($"{work.GetArtistNames()}, \"{work.Name}\": track number {track.Number} title changed from {track.Title} to {mfteo.TitleTag.GetValue<string>()}");
                    track.Title = mfteo.TitleTag.GetValue<string>();
                }
            }
        }
        protected virtual void LoadTags()
        {

        }
        public virtual void AfterDeserialisation(Work work)
        {
            var files = work.Tracks
                .Select(x => (trackNumber: x.Number, musicFile: x.GetBestQuality()));
            var musicFiles = files.Select(x => x.musicFile);
            foreach (var item in TrackList)
            {
                var teoFile = Path.Combine(PathToMusicFiles, item.File);
                var file = musicFiles.Single(x => string.Compare(teoFile, x.File, true) == 0);
                item.MusicFile = file;
            }
        }
        private void LoadCommonMetadata()
        {
            ArtistTag = new TagValueStatus(TagNames.Artist, TrackList.SelectMany(x => x.ArtistTag.Values));
            AlbumTag = new TagValueStatus(TagNames.Album, TrackList.SelectMany(x => x.AlbumTag.Values));
            YearTag = new TagValueStatus(TagNames.Year, TrackList.SelectMany(x => x.YearTag.Values));
        }
    }
    public class TracklistConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType,  object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer,  object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

}
