using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fastnet.Music.Metatools
{
    public abstract class MusicFileTEO // TEO = Tag Editor Object
    {
        public string File { get; set; }
        public long MusicFileId => MusicFile.Id;
        [JsonIgnore]
        public MusicFile MusicFile { get; set; }
        public long? TrackId { get => MusicFile.TrackId;  }
        public TagValueStatus TrackNumberTag { get; set; }
        public TagValueStatus TitleTag { get; set; }
        public TagValueStatus ArtistTag { get; set; }
        public TagValueStatus AlbumTag { get; set; }
        public TagValueStatus YearTag { get; set; }
        protected readonly MusicOptions musicOptions;
        public MusicFileTEO(MusicOptions musicOptions)
        {
            this.musicOptions = musicOptions;
        }
        protected TagValueStatus FindTag(TagNames tagName, ICollection<IdTag> tags)
        {
            var values = new List<string>();
            var item = tags.FirstOrDefault(x => string.Compare(x.Name, tagName.ToString(), true) == 0);
            if (item != null && item.Value.Trim().Length > 0)
            {
                var tagValues = item.Value.Contains("|") ? item.Value.Split("|") : new string[] { item.Value };
                foreach(var tv in tagValues)
                {
                    var temp = musicOptions.ReplaceAlias(tv.Trim());
                    values.Add(temp);
                }
                return new TagValueStatus(tagName, values);
            }
            return null;
        }
        protected virtual void LoadTags()
        {

        }
        public void ResetMetadata()
        {
            LoadCommonMetadata();
            LoadTags();
        }
        private void LoadCommonMetadata()
        {
            var mf = MusicFile;
            var rootPath = Path.Combine(mf.DiskRoot, mf.StylePath, mf.OpusPath);
            var tags = mf.IdTags;
            //MusicFile = mf;
            File = mf.File.Substring(rootPath.Length + 1);
            TrackNumberTag = FindTag(TagNames.TrackNumber, tags);
            TitleTag = FindTag(TagNames.Title, tags);
            AlbumTag = FindTag(TagNames.Album, tags);
            ArtistTag = FindTag(TagNames.Artist, MusicFile.IdTags);
            var yearTvs = FindTag(TagNames.OriginalYear, tags) ?? FindTag(TagNames.Date, tags);

            if (yearTvs != null)
            {
                var availableYears = new List<int>();
                foreach (var tv in yearTvs.Values)
                {
                    var parts = tv.Value.Split(new string[] { " ", ",", "/", "-" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        if (int.TryParse(part, out int year))
                        {
                            if (year > 1900)
                            {
                                availableYears.Add(year);
                            }
                        }
                    }
                }
                YearTag = new TagValueStatus(yearTvs.Tag, availableYears);
            }
        }
        public override string ToString()
        {
            return $"{TrackNumberTag}, {TitleTag}";
        }
    }
}
