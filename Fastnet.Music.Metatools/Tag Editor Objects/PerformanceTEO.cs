using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Fastnet.Music.Metatools;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
//using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fastnet.Music.Metatools
{
    public class PerformanceTEO 
    {
        protected readonly ILogger log;
        protected readonly MusicOptions musicOptions;
        private readonly AccentAndCaseInsensitiveComparer accentsAndCaseInsensitiveComparer = new AccentAndCaseInsensitiveComparer();
        public long PerformanceId { get; set; }
        /// <summary>
        /// Value of COMPOSER tag, valid and in SingleValue if common to all music files
        /// else false and multiple values in MultipleValues
        /// </summary>
        public TagValueStatus ComposerTag { get; set; }
        /// <summary>
        /// Value of COMPOSITION tag, valid and in SingleValue if common to all music files
        /// else false and multiple values in MultipleValues
        /// </summary>
        public TagValueStatus CompositionTag { get; set; }
        /// <summary>
        /// Value of PERFORMERS tag, multiple values in MultipleValues
        /// valid if at least one performer (values matching ORCHESTRA and/or CONDUCTOR removed)
        /// </summary>
        public TagValueStatus PerformerTag { get; set; }
        /// <summary>
        /// Value of ORCHESTRA tag, valid and in SingleValue if common to all music files
        /// else false and multiple values in MultipleValues
        /// SingleValue can be null (which is valid, no Orchestra value is provided) 
        /// </summary>
        public TagValueStatus OrchestraTag { get; set; }
        /// <summary>
        /// Value of CONDUCTOR tag, valid and in SingleValue if common to all music files
        /// else false and multiple values in MultipleValues
        /// SingleValue can be null (which is valid, no Orchestra value is provided) 
        /// </summary>
        public TagValueStatus ConductorTag { get; set; }
        // this list is a subset of the list in the containing WesternClassicalAlbumTEO
        // (1) rename this ?
        // (2) force it somehow to be a subset physically?
        /// <summary>
        /// list of TEO for each movement (subset of TrackFileTEOList)
        /// </summary>
        [JsonIgnore]
        public IEnumerable<WesternClassicalMusicFileTEO> MovementList { get; private set; }
        [JsonProperty]
        public IEnumerable<string> MovementFilenames { get; private set; }
        public PerformanceTEO(MusicOptions musicOptions)
        {
            this.musicOptions = musicOptions;
            log = ApplicationLoggerFactory.CreateLogger(this.GetType());
        }
        public void SetMovementTEOList(IEnumerable<WesternClassicalMusicFileTEO> trackTeos)
        {
            MovementList = trackTeos.Where(t => MovementFilenames.Contains(t.File, StringComparer.CurrentCultureIgnoreCase))
                .OrderBy(t => t.MovementNumberTag.GetValue<int>())
                .ToArray();
        }
        public void LoadTags(IEnumerable<string> movementFilenames, IEnumerable<WesternClassicalMusicFileTEO> trackTeos)
        {
            MovementFilenames = movementFilenames;// fileTeoList.Select(x => x.File);
            SetMovementTEOList(trackTeos);
            var performanceIdList = MovementList.Select(t => t.PerformanceId).Distinct();
            Debug.Assert(performanceIdList.Count() == 1);
            PerformanceId = performanceIdList.First();// ?? 0;
            ComposerTag = new TagValueStatus(TagNames.Composer, MovementList.Where(x => x.ComposerTag != null).SelectMany(x => x.ComposerTag.Values));
            CompositionTag = new TagValueStatus(TagNames.Composition, MovementList.Where(x => x.CompositionTag != null).SelectMany(x => x.CompositionTag.Values));
            OrchestraTag = new TagValueStatus(TagNames. Orchestra, MovementList.Where(x => x.OrchestraTag != null).SelectMany(x => x.OrchestraTag.Values));
            ConductorTag = new TagValueStatus(TagNames.Conductor, MovementList.Where(x => x.ConductorTag != null).SelectMany(x => x.ConductorTag.Values));

            var allPerformers = MovementList
                .Where(x => x.PerformerTag != null)
                .SelectMany(x => x.PerformerTag.Values)
                .Select(x => x.Value)
                .ToList();

            for (int i = 0; i < allPerformers.Count(); ++i)
            {
                var performer = allPerformers[i];
                var p = Regex.Replace(performer, @"\(.*?\)", "").Trim();
                allPerformers[i] = p;
            }
            allPerformers = allPerformers
                .Select(x => musicOptions.ReplaceAlias(x))
                .Distinct(accentsAndCaseInsensitiveComparer)
                .OrderBy(x => x.GetLastName())
                .ToList();
            // now remove any performers that are already defined as orchestras
            foreach (var name in OrchestraTag.Values.Select(x => x.Value))
            {
                var matched = allPerformers.FirstOrDefault(x => x.IsEqualIgnoreAccentsAndCase(name));
                if (matched != null)
                {
                    allPerformers.Remove(matched);
                }
            }
            // now remove any performers that are already defined as conductors
            foreach (var name in ConductorTag.Values.Select(x => x.Value))
            {
                var matched = allPerformers.FirstOrDefault(x => x.IsEqualIgnoreAccentsAndCase(name));
                if (matched != null)
                {
                    allPerformers.Remove(matched);
                }
            }
            PerformerTag = new TagValueStatus(TagNames.Performer, allPerformers, true);
        }
        public void RecordChanges()
        {
            var tracks = MovementList.Select(x => x.MusicFile.Track);
            var performancesInDb = tracks.Select(t => t.Performance).Distinct();
            Debug.Assert(performancesInDb.Count() == 1);
            var performance = performancesInDb.First();
            var composition = performance.Composition;
            if (ComposerTag.GetValue<string>() != composition.Artist.Name)
            {
                log.Information($"{composition.Artist.Name}, \"{composition.Name}\": Artist changed from {composition.Artist.Name} to {ComposerTag.GetValue<string>()}");
                log.Warning("Composer name changes are not supported");
            }
            if (CompositionTag.GetValue<string>() != composition.Name)
            {
                log.Information($"{composition.Artist.Name}, \"{composition.Name}\": Composition changed from {composition.Name} to {CompositionTag.GetValue<string>()}");
                composition.Name = CompositionTag.GetValue<string>();
            }
            var performerList = PerformerTag.GetValues<string>().ToList();
            performerList.AddRange(OrchestraTag.GetValues<string>());
            performerList.AddRange(ConductorTag.GetValues<string>());
            var performers = string.Join(", ", performerList);
            if (performers != performance.Performers)
            {
                log.Information($"{composition.Artist.Name}, \"{composition.Name}\": Performers changed from {performance.Performers} to {performers}");
                performance.Performers = performers;
            }
            foreach (var m in MovementList)
            {
                var t = tracks.First(x => m.TrackId == x.Id);
                if(m.MovementNumberTag.GetValue<int>() != t.MovementNumber)
                {
                    log.Information($"{composition.Artist.Name}, \"{composition.Name}\", \"{performance.Performers}\": movement number changed from {t.MovementNumber} to {m.MovementNumberTag.GetValue<int>()}");
                    log.Warning("Movement number changes are not supported");
                }
                if (m.TitleTag.GetValue<string>() != t.Title)
                {
                    log.Information($"{composition.Artist.Name}, \"{composition.Name}\", \"{performance.Performers}\": title changed changed from {t.Title} to {m.TitleTag.GetValue<string>()}");
                    t.Title = m.TitleTag.GetValue<string>();
                }
            }
        }
    }
}
