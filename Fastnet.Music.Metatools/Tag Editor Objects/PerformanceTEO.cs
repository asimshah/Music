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
        //public IEnumerable<WesternClassicalMusicFileTEO> MovementList { get; private set; }
        public IEnumerable<MusicFileTEO> MovementList { get; private set; }
        [JsonProperty]
        public IEnumerable<string> MovementFilenames { get; private set; }
        public PerformanceTEO(MusicOptions musicOptions)
        {
            this.musicOptions = musicOptions;
            log = ApplicationLoggerFactory.CreateLogger(this.GetType());
        }
        //public void SetMovementTEOList(IEnumerable<WesternClassicalMusicFileTEO> trackTeos)
        public void SetMovementTEOList(IEnumerable<MusicFileTEO> trackTeos)
        {
            MovementList = trackTeos.Where(t => MovementFilenames.Contains(t.File, StringComparer.CurrentCultureIgnoreCase))
                .OrderBy(t => t.MovementNumberTag.GetValue<int>())
                .ToArray();
        }
        //public void LoadTags(IEnumerable<string> movementFilenames, IEnumerable<WesternClassicalMusicFileTEO> trackTeos)
        public void LoadTags(IEnumerable<string> movementFilenames, IEnumerable<MusicFileTEO> trackTeos)
        {

            MovementFilenames = movementFilenames;// fileTeoList.Select(x => x.File);
            SetMovementTEOList(trackTeos);
            var performanceIdList = MovementList.Select(t => t.PerformanceId).Distinct();
            Debug.Assert(performanceIdList.Count() == 1);
            PerformanceId = performanceIdList.First();// ?? 0;
            //if (PerformanceId == 131)
            //{
            //    Debugger.Break();
            //}
            ComposerTag = new TagValueStatus(TagNames.Composer, MovementList.Where(x => x.ComposerTag != null).SelectMany(x => x.ComposerTag.Values));
            CompositionTag = new TagValueStatus(TagNames.Composition, MovementList.Where(x => x.CompositionTag != null).SelectMany(x => x.CompositionTag.Values));
            OrchestraTag = new TagValueStatus(TagNames. Orchestra, MovementList.Where(x => x.OrchestraTag != null).SelectMany(x => x.OrchestraTag.Values));
            ConductorTag = new TagValueStatus(TagNames.Conductor, MovementList.Where(x => x.ConductorTag != null).SelectMany(x => x.ConductorTag.Values));

            var allPerformers = MovementList
                .Where(x => x.PerformerTag != null)
                .SelectMany(x => x.PerformerTag.Values)
                .Select(x => x.Value)
                .ToList();
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
        public void RecordChanges(MusicDb db)
        {
            bool compareTagsWithPerformers(TagValueStatus tvs, IEnumerable<PerformancePerformer> performerSet)
            {
                var r = CompareTwoArrays(tvs.Values.Select(x => x.Value), performerSet.Select(x => x.Performer.Name));
                if(r)
                {
                    // the names are the same though not necessarily in the same order
                    // so now compare selection state
                    foreach(var ps in performerSet)
                    {
                        var tv = tvs.Values.Single(x => string.Compare(x.Value, ps.Performer.Name, true) == 0);
                        if(tv.Selected != ps.Selected)
                        {
                            r = false;
                            break;
                        }
                    }
                }
                return r;
            }
            //IEnumerable<string> SplitString(string text)
            //{
            //    return text.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
            //}
            bool CompareTwoArrays(IEnumerable<string> left, IEnumerable<string> right)
            {
                if(left.Count() == right.Count())
                {
                    var intersection = left.Intersect(right, StringComparer.CurrentCultureIgnoreCase);
                    return intersection.Count() == left.Count();
                }
                return false;
            }
            void updatePerformerSet(Performance performance, TagValueStatus tvs, PerformerType type)
            {
                //var subSet = performance.GetPerformerSubSet(type);
                foreach (var tv in tvs.Values)
                {
                    var pp = performance.GetPerformancePerformerSubSet(type).SingleOrDefault(x => string.Compare(x.Performer.Name, tv.Value) == 0);                    
                    if(pp != null)
                    {
                        pp.Selected = tv.Selected;
                    }
                    else
                    {
                        // this is a new name for this performance
                        var performer  = db.GetPerformer(tv.Value, type);
                        performance.PerformancePerformers.Add(new PerformancePerformer { Performance = performance, Performer = performer, Selected = tv.Selected });
                    }
                }
                // now check if any performers have been removed
                // *NB* at present there is no feature planned that allows performers to be removed via tag editing...
                var missingNames = performance.PerformancePerformers
                    .Select(pp => pp.Performer.Name)
                    .Except(tvs.GetValues<string>(), StringComparer.CurrentCultureIgnoreCase);
                foreach(var name in missingNames)
                {
                    var pp = performance.PerformancePerformers.Single(x => string.Compare(x.Performer.Name, name, true) == 0);
                    var performer = pp.Performer;
                    performance.PerformancePerformers.Remove(pp);
                    db.PerformancePerformers.Remove(pp);
                    if (db.PerformancePerformers.Where(x => x.Performer == performer).Count() == 0)
                    {
                        db.Performers.Remove(performer);
                    }
                }
            }
            var tracks = MovementList.Select(x => x.MusicFile.Track);
            var performancesInDb = tracks.Select(t => t.Performance).Distinct();
            Debug.Assert(performancesInDb.Count() == 1);
            var performance = performancesInDb.First();

            var composition = performance.Composition;
            if (!ComposerTag.GetValue<string>().IsEqualIgnoreAccentsAndCase(composition.Artist.Name))
            {
                log.Information($"[A-{composition.Artist.Id}] {composition.Artist.Name}, [C-{composition.Id}] \"{composition.Name}\" [P-{performance.Id}]: Artist changed from {composition.Artist.Name} to {ComposerTag.GetValue<string>()}");
                log.Warning("Composer name changes are not supported");
            }
            if (!CompositionTag.GetValue<string>().IsEqualIgnoreAccentsAndCase(composition.Name))
            {
                log.Information($"{composition.Artist.Name}, \"{composition.Name}\": Composition changed from {composition.Name} to {CompositionTag.GetValue<string>()}");
                var existingCompositions = composition.Artist.Compositions;
                var newName = CompositionTag.GetValue<string>();
                var existingComposition = existingCompositions.SingleOrDefault(x => x.Name.IsEqualIgnoreAccentsAndCase(newName));
                if (existingComposition != null)
                {
                    // this performance needs to be moved into the existing one
                    // *NB* it is possible, though unlikely that a performance of exactly the same performers already exists!
                    // for the present I am not checking for this!!
                    var oldComposition = performance.Composition;
                    performance.Composition = existingComposition;
                    oldComposition.Performances.Remove(performance); 
                    //existingComposition.Performances.Add(performance);
                    if(oldComposition.Performances.Count() == 0)
                    {
                        db.Compositions.Remove(oldComposition);
                    }
                    var exists = db.Performances.SingleOrDefault(x => x.Id == performance.Id) != null;
                    if(!exists)
                    {
                        Debugger.Break();
                    }
                    log.Information($"performance [P-{performance.Id}] moved from composition [C-{oldComposition.Id}] to composition [C-{existingComposition.Id}]");
                    log.Information($"composition [C-{existingComposition.Id}] now has {existingComposition.Performances.Count()} performances:");
                    foreach(var p in existingComposition.Performances)
                    {
                        log.Information($"performance [P-{p.Id}] \"{p.GetAllPerformersCSV()}\"");
                    }
                    foreach(var m in performance.Movements)
                    {
                        m.CompositionName = performance.Composition.Name;
                        if (m.Title.Contains(":"))
                        {
                            var parts = m.Title.Split(":");
                            if (parts[0].IsEqualIgnoreAccentsAndCase(m.CompositionName))
                            {
                                m.Title = string.Join(":", parts.Skip(1));
                            }
                        }
                    }
                }
                else
                {
                    composition.Name = newName; // CompositionTag.GetValue<string>();
                }
            }
            //if(!CompareTwoArrays(OrchestraTag.GetValues<string>(false), SplitString(performance.Orchestras)))
            if(!compareTagsWithPerformers(OrchestraTag, performance.GetPerformancePerformerSubSet(PerformerType.Orchestra)))
            {
                updatePerformerSet(performance, OrchestraTag, PerformerType.Orchestra);
                //var orchestras = string.Join(", ", OrchestraTag.GetValues<string>(false));
                //log.Information($"[A-{composition.Artist.Id}] {composition.Artist.Name}, [C-{composition.Id}] \"{composition.Name}\" [P-{performance.Id}]: Orchestras changed from {performance.Orchestras} to {orchestras}");
                //performance.Orchestras = orchestras;
            }
            //if (!CompareTwoArrays(ConductorTag.GetValues<string>(false), SplitString(performance.Conductors)))
            if (!compareTagsWithPerformers(ConductorTag, performance.GetPerformancePerformerSubSet(PerformerType.Conductor)))
            {
                updatePerformerSet(performance, ConductorTag, PerformerType.Conductor);
                //var conductors = string.Join(", ", ConductorTag.GetValues<string>(false));
                //log.Information($"[A-{composition.Artist.Id}] {composition.Artist.Name}, [C-{composition.Id}] \"{composition.Name}\" [P-{performance.Id}]: Conductors changed from {performance.Conductors} to {conductors}");
                //performance.Conductors = conductors;
            }
            //if (!CompareTwoArrays(PerformerTag.GetValues<string>(false), SplitString(performance.Performers)))
            if (!compareTagsWithPerformers(PerformerTag, performance.GetPerformancePerformerSubSet(PerformerType.Other)))
            {
                updatePerformerSet(performance, PerformerTag, PerformerType.Other);
                //var performers = string.Join(", ", PerformerTag.GetValues<string>());
                //log.Information($"[A-{composition.Artist.Id}] {composition.Artist.Name}, [C-{composition.Id}] \"{composition.Name}\" [P-{performance.Id}]: Performers changed from {performance.Performers} to {performers}");
                //performance.Performers = performers;
            }
            foreach (var m in MovementList)
            {
                var t = tracks.First(x => m.TrackId == x.Id);
                if(m.MovementNumberTag.GetValue<int>() != t.MovementNumber)
                {
                    log.Information($"[A-{composition.Artist.Id}] {composition.Artist.Name}, [C-{composition.Id}] \"{composition.Name}\",  [P-{performance.Id}] \"{performance.GetAllPerformersCSV()}\": movement number changed from {t.MovementNumber} to {m.MovementNumberTag.GetValue<int>()}");
                    log.Warning("Movement number changes are not supported");
                }
                if (m.TitleTag.GetValue<string>() != t.Title)
                {
                    log.Information($"[A-{composition.Artist.Id}] {composition.Artist.Name}, [C-{composition.Id}] \"{composition.Name}\",  [P-{performance.Id}] \"{performance.GetAllPerformersCSV()}\": title changed changed from {t.Title} to {m.TitleTag.GetValue<string>()}");
                    t.Title = m.TitleTag.GetValue<string>();
                }
            }
        }
    }
}
