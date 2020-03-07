using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using System.Collections.Generic;
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
    public class WesternClassicalMusicFileTEO : MusicFileTEO // TEO = Tag Editor Object
    {


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
        public WesternClassicalMusicFileTEO(MusicOptions musicOptions) : base(musicOptions)
        {
        }
        protected override void LoadTags()
        {
            //base.LoadTags();
            var mf = MusicFile;
            PerformanceId = mf.Track.Performance.Id;
            MovementNumberTag = new TagValueStatus(TagNames.MovementNumber, mf.Track.MovementNumber);
            var tags = mf.IdTags;
            TagValueStatus mergeTags(TagNames outputTag, params string[] keys)
            {
                var tvs = FindTag(outputTag, tags);
                if (tvs != null)
                {
                    var values = new List<string>(tvs.Values.Select(x => x.Value));
                    foreach (var key in keys)
                    {
                        values.AddRange(findValues(key, tags));
                    }
                    IEnumerable<string> excludeList = new string[0];
                    if(OrchestraTag != null)
                    {
                        excludeList = OrchestraTag.Values.Select(x => x.Value);
                    }
                    if(ConductorTag != null)
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
        private List<string> findValues(string tagName, ICollection<IdTag> tags)
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
            return values;
        }
        public override string ToString()
        {
            return $"{MovementNumberTag}, {TitleTag}";
        }
    }

}
