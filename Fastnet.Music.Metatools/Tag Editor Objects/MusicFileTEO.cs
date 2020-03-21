using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Fastnet.Music.Metatools
{
    class LastnameComparer : AccentAndCaseInsensitiveComparer
    {
        public override int Compare(string x, string y)
        {
            return base.Compare(x.GetLastName(), y.GetLastName());
        }
        public override int GetHashCode(string obj)
        {
            return base.GetHashCode(obj.GetLastName());
        }
    }
    public /*abstract*/ class MusicFileTEO // TEO = Tag Editor Object
    {
        public MusicStyles MusicStyle => MusicFile.Style;
        public string File { get; set; }
        public long MusicFileId => MusicFile.Id;
        [JsonIgnore]
        public MusicFile MusicFile { get; set; }
        public long? TrackId { get => MusicFile.TrackId; }
        public TagValueStatus TrackNumberTag { get; set; }
        public TagValueStatus TitleTag { get; set; }
        public TagValueStatus ArtistTag { get; set; }
        public TagValueStatus AlbumTag { get; set; }
        public TagValueStatus YearTag { get; set; }

        public long PerformanceId { get; set; }
        public TagValueStatus ComposerTag { get; set; }
        public TagValueStatus CompositionTag { get; set; }
        /// <summary>
        /// all performers found in id tags "PERFORMER", "PERFORMER", "PERFORMERS", "SOLOISTS"
        /// as MultipleValues (not including Orchestra and Conductor)
        /// </summary>
        public TagValueStatus PerformerTag { get; set; }
        public TagValueStatus OrchestraTag { get; set; }
        public TagValueStatus ConductorTag { get; set; }
        public TagValueStatus MovementNumberTag { get; set; }


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
                foreach (var tv in tagValues)
                {
                    var temp = musicOptions.ReplaceAlias(tv.Trim());
                    values.Add(temp);
                }
                return new TagValueStatus(tagName, values);
            }
            return null;
        }
        protected /*virtual*/ void LoadTags()
        {
            if (MusicStyle == MusicStyles.WesternClassical)
            {
                LoadWesternClassicalTags();
            }
        }
        public void ResetMetadata()
        {
            LoadCommonMetadata();
            LoadTags();
            if (TrackNumberTag.GetValue<int>() != MusicFile.Track.Number)
            {
                // the idtag value for the track number is not necessarily correct
                // for example, this music file might be part of a multi-part set
                TrackNumberTag.SetValue(MusicFile.Track.Number);
            }
        }
        private void LoadCommonMetadata()
        {
            var mf = MusicFile;
            //var rootPath = Path.Combine(mf.DiskRoot, mf.StylePath, mf.OpusPath);
            //var rootPath = mf.OpusType == OpusType.Collection ?
            //    Path.Combine(mf.DiskRoot, mf.StylePath, "Collections", mf.OpusPath)
            //    : Path.Combine(mf.DiskRoot, mf.StylePath, mf.OpusPath);
            var rootPath = mf.GetRootPath();
            var tags = mf.IdTags;
            File = mf.File.Substring(rootPath.Length + 1);
            TrackNumberTag = FindTag(TagNames.TrackNumber, tags);
            TitleTag = FindTag(TagNames.Title, tags);
            if (TitleTag.GetValue<string>().Contains(':'))
            {
                var workName = MusicStyle == MusicStyles.WesternClassical ?
                    mf.Track.Performance.Composition.Name : mf.Track.Work.Name;
                var parts = TitleTag.GetValue<string>().Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (parts[0].IsEqualIgnoreAccentsAndCase(workName))
                {
                    var title = string.Join(":", parts.Skip(1)).Trim();
                    TitleTag.SetValue(title);
                }
            }
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

        private void LoadWesternClassicalTags()
        {
            //base.LoadTags();
            //if(MusicFile.File.Contains("Turca"))
            //{
            //    Debugger.Break();
            //}
            var mf = MusicFile;
            PerformanceId = mf.Track.Performance.Id;
            MovementNumberTag = new TagValueStatus(TagNames.MovementNumber, mf.Track.MovementNumber);
            var tags = mf.IdTags;
            IEnumerable<string> findValues(string tagName, ICollection<IdTag> tags)
            {
                var values = new List<string>();
                var item = tags.FirstOrDefault(x => string.Compare(x.Name, tagName, true) == 0);
                if (item != null)
                {
                    if (item.Value.Contains("|"))
                    {
                        values.AddRange(item.Value.Split("|"));
                    }
                    else if (item.Value.Trim().Length > 0)
                    {
                        values.Add(item.Value.Trim());
                    }
                }
                return values.Select(v => v.StripBracketedContent());
            }
            TagValueStatus mergeTags(TagNames outputTag, params string[] keys)
            {
                var tvs = FindTag(outputTag, tags);
                if (tvs != null)
                {
                    var values = new List<string>(tvs.Values.Select(x => x.Value.StripBracketedContent()));
                    foreach (var key in keys)
                    {
                        values.AddRange(findValues(key, tags));
                    }

                    IEnumerable<string> excludeList = new string[0];
                    if (OrchestraTag != null)
                    {
                        excludeList = OrchestraTag.Values.Select(x => x.Value);
                    }
                    if (ConductorTag != null)
                    {
                        excludeList = excludeList.Union(ConductorTag.Values.Select(x => x.Value));
                    }
                    //var excludeList = OrchestraTag?.Values.Select(x => x.Value)
                    //    .Union(ConductorTag?.Values.Select(x => x.Value));
                    tvs = new TagValueStatus(outputTag, values
                        .Except(excludeList, new LastnameComparer())
                        .OrderBy(x => x.GetLastName()));
                }
                return tvs;
            }
            ComposerTag = FindTag(TagNames.Composer, tags) ?? FindTag(TagNames.Artist, tags);
            CompositionTag = FindTag(TagNames.Composition, tags) ?? FindTag(TagNames.Work, tags);
            ConductorTag = FindTag(TagNames.Conductor, tags);
            OrchestraTag = FindTag(TagNames.Orchestra, tags);
            PerformerTag = mergeTags(TagNames.Performer, "PERFORMER", "PERFORMERS", "SOLOISTS");
        }
    }
}
