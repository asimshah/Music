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
        //private string BuildPerformerCSV()
        //{
        //    var comparer = new AccentAndCaseInsensitiveComparer();
        //    try
        //    {
        //        var names = new List<string>();
        //        names.AddRange(otherPerformers);
        //        return string.Join(", ", names);
        //    }
        //    catch (Exception)
        //    {
        //        Debugger.Break();
        //        throw;
        //    }
        //}
        public override async Task<CatalogueResult> CatalogueAsync()
        {
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
                return new CatalogueResult { MusicSet = this, Status = CatalogueStatus.Success, Artist = composer, Composition = composition, Performance = performance };
            }
            else
            {
                var performance = FirstFile.Track.Performance;
                return new CatalogueResult { MusicSet = this, Status = CatalogueStatus.Success, Artist = performance.Composition.Artist, Composition = performance.Composition, Performance = performance };
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
                    log.Warning($"  {performance.Composition.Artist.Name}, {performance.Composition.Name}, {performance.Performers}");
                }
            }
            foreach(var performance in performances.ToArray())
            {
                MusicDb.Performances.Remove(performance);
                log.Information($"{performance.Composition.Artist.Name}, {performance.Composition.Name}, {performance.Performers} removed");
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
        private Performance GetPerformance(Composition composition, IEnumerable<string> orchestraList, IEnumerable<string> conductorList, IEnumerable<string> performerList)
        {
            Debug.Assert(MusicDb != null);
            string orchestras = orchestraList.ToCSV();
            string conductors = conductorList.ToCSV();
            string performers = performerList.ToCSV();
            int year = FirstFile.GetYear() ?? 0;
            var performance = composition.Performances.SingleOrDefault(p =>
                p.Orchestras.IsEqualIgnoreAccentsAndCase(orchestras)
                && p.Conductors.IsEqualIgnoreAccentsAndCase(conductors)
                && p.Performers.IsEqualIgnoreAccentsAndCase(performers)
                && p.Year == year);
            //if (performance != null)
            //{
            //    // find a unique name for this performance
            //    var index = 1;
            //    var found = false;
            //    while (!found)
            //    {
            //        var name = $"{performers} ({++index})";
            //        performance = composition.Performances.SingleOrDefault(p => p.Performers.IsEqualIgnoreAccentsAndCase(name));
            //        found = performance == null;
            //    }
            //}
            var alphaMeric = performerList.Union(orchestraList).Union(conductorList).ToCSV().ToAlphaNumerics();
            performance = new Performance
            {
                Composition = composition,
                Orchestras = orchestras,
                Conductors = conductors,
                Performers = performers,
                AlphamericPerformers = alphaMeric,// performers.ToAlphaNumerics(),
                Year = year //FirstFile.GetYear() ?? 0
            };
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
