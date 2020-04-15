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
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Each western classical music set is catalogued by artist, composition and performance where the performers string distinguishes
    /// individual performances. Each performance has movements (which are a subset of the tracks whic have been previously
    /// catalogued as a western classical album)
    /// </summary>
    public class WesternClassicalCompositionSet : MusicSetWithPerformances //MusicSet 
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
        protected override string GetName()
        {
            return $"{ComposerName}:{CompositionName}";
        }
        private IEnumerable<string> GetConductors()
        {
            return MusicFiles
                .SelectMany(x => x.GetConductors())
                .Select(x => MusicOptions.ReplaceAlias(x))
                .Distinct(comparer)
                .OrderBy(x => x.GetLastName());
        }
        private IEnumerable<string> GetOrchestras()
        {
            return MusicFiles
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
        public override async Task<CatalogueResultBase> CatalogueAsync()
        {
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
                return new WesternClassicalCompositionCatalogueResult(this, CatalogueStatus.Success, performance);// { MusicSet = this, Status = CatalogueStatus.Success, Artist = composer, Composition = composition, Performance = performance };
            }
            else
            {
                var performance = FirstFile.Track.Performance;
                return new WesternClassicalCompositionCatalogueResult(this, CatalogueStatus.Success, performance);// { MusicSet = this, Status = CatalogueStatus.Success, Artist = performance.Composition.Artist, Composition = performance.Composition, Performance = performance };
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
            //int year = FirstFile.GetYear() ?? 0;
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


            MusicDb.AddPerformance(composition, performance);
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
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
