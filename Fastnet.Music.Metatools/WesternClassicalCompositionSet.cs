using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Music.Metatools
{
    /// <summary>
    /// Each western classical music set is catalogued by artist, composition and performance where the performers string distinguishes
    /// individual performances. Each performance has movements (which are a subset of the tracks whic have been previously
    /// catalogued as a western classical album)
    /// </summary>
    public class WesternClassicalCompositionSet : MusicSet //<WesternClassicalMusicTags>
    {
        private AccentAndCaseInsensitiveComparer comparer = new AccentAndCaseInsensitiveComparer();
        //private PerformanceTEO performanceTEO;
        private IEnumerable<string> conductors;
        private IEnumerable<string> orchestras;
        private IEnumerable<string> otherPerformers;
        private string ComposerName { get; set; }
        public string CompositionName { get; set; }
        public WesternClassicalCompositionSet(MusicDb db, MusicOptions musicOptions, string composerName,
            string compositionName, IEnumerable<MusicFile> musicFiles, TaskItem taskItem) : base(db, musicOptions, MusicStyles.WesternClassical, musicFiles, taskItem)
        {
            var fileNameList = musicFiles.Select(x => Path.GetFileName(x.File));
            this.ComposerName = musicOptions.ReplaceAlias(composerName);
            this.CompositionName = musicOptions.ReplaceAlias(compositionName);
            //**NB** following calls must remain in this order!!
            conductors = GetConductors();
            orchestras = GetOrchestras();
            otherPerformers = GetOtherPerformers();
        }
        //protected override async Task LoadMusicTags()
        //{
        //    var json = await ReadMusicTagJson();
        //    if (json != null)
        //    {
        //        var teo = json.ToInstance<WesternClassicalAlbumTEO>();
        //        performanceTEO = teo.PerformanceList.Single(x =>
        //            x.MovementFilenames.All(f => MusicFiles.Select(mf => mf.File).SingleOrDefault(x => x.EndsWith(f, System.Globalization.CompareOptions.IgnoreCase)) != null)
        //        );
        //        ComposerName = performanceTEO.ComposerTag.GetValue<string>();
        //        CompositionName = performanceTEO.CompositionTag.GetValue<string>();
        //        orchestras = performanceTEO.OrchestraTag.GetValues<string>();
        //        conductors = performanceTEO.ConductorTag.GetValues<string>();
        //        otherPerformers = performanceTEO.PerformerTag.GetValues<string>();
        //    }
        //}
        protected override string GetName()
        {
            return $"{ComposerName}:{CompositionName}";
        }
        private IEnumerable<string> GetConductors()
        {
            return MusicFiles
                //.Where(x => x.GetConductor() != null)
                .SelectMany(x => x.GetConductors())
                .Select(x => MusicOptions.ReplaceAlias(x))
                .Distinct(comparer)
                .OrderBy(x => x.GetLastName());
        }
        private IEnumerable<string> GetOrchestras()
        {
            return MusicFiles
                //.Where(x => x.GetOrchestra() != null)
                .SelectMany(x => x.GetOrchestras())
                .Select(x => MusicOptions.ReplaceAlias(x))
                .Distinct(comparer)
                .OrderBy(x => x);
        }
        private IEnumerable<string> GetOtherPerformers()
        {
            var _names = new List<string>();
            foreach (var mf in MusicFiles)
            {
                var list = mf.GetPerformers()
                    .Select(x => MusicOptions.ReplaceAlias(x))
                    .Except(new string[] { MusicOptions.ReplaceAlias(mf.Musician) }, comparer)
                    ;
                _names.AddRange(list);
            }
            //if(_names.Any(n => n.Contains("&")))
            //{
            //    Debugger.Break();
            //}
            _names = _names.Distinct(comparer).ToList();
            var g1 = _names.GroupBy(x => x.GetLastName());
            var names = new List<string>();
            foreach (var item in g1)
            {
                if (!ComposerName.EndsWithIgnoreAccentsAndCase(item.Key))
                {
                    names.Add(item.OrderByDescending(x => x.Length).First());
                }
            }
            names = names
                .OrderBy(x => x.GetLastName())
                .ToList();
            foreach (var orchestra in orchestras)
            {
                names = RemoveName(names, orchestra).ToList();
            }
            foreach (var conductor in conductors)
            {
                names = RemoveName(names, conductor).ToList();
            }
            
            return names;
        }
        public override async Task<CatalogueResult> CatalogueAsync()
        {
            //void makeSafeForReporting(Performance performance)
            //{
            //    MusicDb.Entry(performance).Collection(x => x.PerformancePerformers).Load();
            //    foreach(var item in performance.PerformancePerformers)
            //    {
            //        MusicDb.Entry(item).Reference(x => x.Performer).Load();
            //    }
            //};
            //await LoadMusicTags();
            Debug.Assert(!string.IsNullOrWhiteSpace(ComposerName));
            Debug.Assert(!string.IsNullOrWhiteSpace(CompositionName));
            Debug.Assert(FirstFile != null);
            Debug.Assert(FirstFile.Track != null);
            // make sure tracks have not already been catalogued as being for a performance
            // this happens when additional music files are found for the same album and work
            if (FirstFile.Track.Performance == null)
            {
                var composer = await GetArtistAsync(ComposerName);
                var composition = GetComposition(composer, CompositionName);
                RemoveCurrentPerformance();
                var performance = GetPerformance(composition, orchestras, conductors, otherPerformers);
                //makeSafeForReporting(performance);
                return CatalogueResult.Create(this, CatalogueStatus.Success, performance);// { MusicSet = this, Status = CatalogueStatus.Success, Artist = composer, Composition = composition, Performance = performance };
            }
            else
            {
                var performance = FirstFile.Track.Performance;
                //makeSafeForReporting(performance);
                return CatalogueResult.Create(this, CatalogueStatus.Success, performance);// { MusicSet = this, Status = CatalogueStatus.Success, Artist = performance.Composition.Artist, Composition = performance.Composition, Performance = performance };
            }
        }

        private void RemoveCurrentPerformance()
        {
            // find the performance containing the current music files, if any
            var tracks = this.MusicFiles.Where(x => x.Track != null).Select(x => x.Track);
            var performances = tracks.Where(t => t.Performance != null).Select(t => t.Performance);
            if(performances.Count() > 1)
            {
                var idlist = string.Join(", ", this.MusicFiles.Select(x => x.Id));
                log.Warning($"Music files {idlist} have more than one performance - this is unexpected!");
                foreach(var performance in performances)
                {
                    log.Warning($"  {performance.Composition.Artist.Name}, {performance.Composition.Name}, {performance.GetAllPerformersCSV()}");
                }
            }
            foreach(var performance in performances.ToArray())
            {
                var performers = performance.GetAllPerformersCSV();
                MusicDb.PerformancePerformers.RemoveRange(performance.PerformancePerformers);
                MusicDb.Performances.Remove(performance);
                log.Information($"{performance.Composition.Artist.Name}, {performance.Composition.Name}, {performance.GetAllPerformersCSV()} removed");
            }
        }

        private Composition FindComposition(Artist artist, string name)
        {
            try
            {
                var alphaNumericName = name.ToAlphaNumerics();
                return artist.Compositions.SingleOrDefault(w => string.Compare(w.Name.ToAlphaNumerics(), alphaNumericName, true) == 0);
            }
            catch (Exception xe)
            {
                log.Error($"{xe.Message} looking for {artist.Name}, {name} (as {name.ToAlphaNumerics()})");
                throw;
            }
        }
        private Composition GetComposition(Artist artist, string name)
        {
            Debug.Assert(MusicDb != null);
            //if(name.ToAlphaNumerics() == "SevenSongsOp95")
            //{
            //    Debugger.Break();
            //}
            var composition = FindComposition(artist, name);// artist.Compositions.SingleOrDefault(w => w.Name.IsEqualIgnoreAccentsAndCase(name));
            if (composition == null)
            {
                composition = new Composition
                {
                    Artist = artist,
                    Name = name,
                    AlphamericName = name.ToAlphaNumerics()
                };
                artist.Compositions.Add(composition);
            }
            return composition;
        }
        private IEnumerable<Performer> GetPerformers(IEnumerable<string> names, PerformerType type)
        {
            var list = new List<Performer>();
            foreach(var name in names)
            {

                var performer = MusicDb.Performers
                    .Where(p => p.Type == type)
                    .ToArray()
                    .SingleOrDefault(p => p.Name.IsEqualIgnoreAccentsAndCase(name));
                if(performer == null)
                {
                    performer = new Performer
                    {
                        Name = name,
                        Type = type
                    };
                    MusicDb.Performers.Add(performer);
                }
                list.Add(performer);
            }
            return list;
        }
        private Performance GetPerformance(Composition composition, IEnumerable<string> orchestraNames, IEnumerable<string> conductorNames, IEnumerable<string> otherNames)
        {
            Debug.Assert(MusicDb != null);
            var performers = otherNames.Union(orchestraNames).Union(conductorNames).ToCSV();
            var conductors = MusicDb.FindPerformers(conductorNames, PerformerType.Conductor);
            var orchestras = MusicDb.FindPerformers(orchestraNames, PerformerType.Orchestra);
            var others = MusicDb.FindPerformers(otherNames, PerformerType.Other);
            int year = FirstFile.GetYear() ?? 0;
            if(conductors.Count() == conductorNames.Count() && orchestras.Count() == orchestraNames.Count() && others.Count() == otherNames.Count())
            {
                var performances = composition.Performances
                    .Where(p => p.Year == year)
                    .ToArray()
                    .Where(p => p.GetPerformancePerformerSubSet(PerformerType.Conductor).Select(x => x.Performer).Union(conductors).Count() == 0
                    && p.GetPerformancePerformerSubSet(PerformerType.Orchestra).Select(x => x.Performer).Union(orchestras).Count() == 0
                    && p.GetPerformancePerformerSubSet(PerformerType.Other).Select(x => x.Performer).Union(others).Count() == 0);
                if (performances.Count() > 0)
                {
                    log.Warning($"[C-{composition.Id}] performers {performers}, {performances.Count()} existing performance(s) found:");
                    foreach (var p in performances)
                    {
                        log.Warning($"   [P-{p.Id}]");
                    }
                }
            }
            var performance = new Performance
            {
                Composition = composition,
                AlphamericPerformers = performers.ToAlphaNumerics(),
                Year = year
            };

            performance.PerformancePerformers
                .AddRange(MusicDb.GetPerformers(conductorNames, PerformerType.Conductor)
                    .Select(p => new PerformancePerformer { Performer = p, Performance = performance, Selected = true }));
            performance.PerformancePerformers
                .AddRange(MusicDb.GetPerformers(orchestraNames, PerformerType.Orchestra)
                    .Select(p => new PerformancePerformer { Performer = p, Performance = performance, Selected = true }));
            performance.PerformancePerformers
                .AddRange(MusicDb.GetPerformers(otherNames, PerformerType.Other)
                    .Select(p => new PerformancePerformer { Performer = p, Performance = performance, Selected = true }));

            var movementNumber = 0;
            foreach (var track in MusicFiles.Select(mf => mf.Track).OrderBy(x => x.Number))
            {
                if (track == null || track.Performance != null)
                {
                    Debugger.Break();
                }
                track.MovementNumber = ++movementNumber;
                performance.Movements.Add(track);
            }
            Debug.Assert(performance.Movements.Count > 0);
            composition.Performances.Add(performance);
            return performance;
        }
        private IEnumerable<string> RemoveDuplicateNames(IEnumerable<string> names)
        {
            var comparer = new AccentAndCaseInsensitiveComparer();
            var lastNames = names.Select(x => x.GetLastName()).Distinct(comparer);
            var namesToRemove = new List<string>();
            foreach (var ln in lastNames)
            {
                var commonLastNames = names.Where(x => x.EndsWithIgnoreAccentsAndCase(ln));
                if (commonLastNames.Count() > 1)
                {
                    var longestLength = commonLastNames.Max(x => x.Length);
                    namesToRemove.AddRange(commonLastNames.Where(x => x.Length < longestLength));
                }
            }
            namesToRemove.AddRange(names.Where(x => ComposerName.EndsWithIgnoreAccentsAndCase(x)));
            return names.Except(namesToRemove);//.ToList();
        }
        private IEnumerable<string> RemoveName(IEnumerable<string> names, string name)
        {
            name = name.GetLastName();
            var namesToRemove = new List<string>();
            foreach(var n in names.Where(x => x.StartsWithIgnoreAccentsAndCase(name) || x.EndsWithIgnoreAccentsAndCase(name)))
            {
                namesToRemove.Add(n);
            }
            return names.Except(namesToRemove);
        }
        public override string ToString()
        {
            return $"{this.GetType().Name}::{ComposerName}::{CompositionName}::{MusicFiles.Count()} files";
        }
    }
}
