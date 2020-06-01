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
    public class IndianClassicalRagaSet : BasePerformanceSet
    {
        private readonly string ragaName;
        private readonly IndianClassicalInformation ici;
        internal IndianClassicalRagaSet(MusicDb db, MusicOptions musicOptions, IndianClassicalInformation ici,
            IEnumerable<MusicFile> musicFiles, TaskItem taskItem)
            : base(db, musicOptions, MusicStyles.IndianClassical, musicFiles, taskItem)
        {
            this.ici = ici;
            //this.ici.PrepareNames();
            var ragaNames = musicFiles.Select(x => x.GetRagaName()).Distinct();
            Debug.Assert(ragaNames.Count() == 1, $"{taskItem} music files have more than one raga name");
            this.ragaName = ragaNames.First();
        }
        public async override Task<BaseCatalogueResult> CatalogueAsync()
        {
            RemoveCurrentPerformance();
            var Raga = GetRaga(ragaName);
            List<Artist> Artists = new List<Artist>();
            foreach (var performer in otherPerformers.ToArray())
            {
                var artist = FindArtist(performer.Name);
                if(artist != null)
                {
                    otherPerformers.Remove(performer);
                    performer.Reset(PerformerType.Artist);
                    artistPerformers.Add(performer);
                    log.Information($"{performer} moved from otherPerformers to artistPerformers");
                }
            }
            foreach (var ap in artistPerformers)
            {
                var artist = await GetArtistAsync(ap);
                Artists.Add(artist);
            }
            var performers = GetPerformers(otherPerformers);
            var performance = GetPerformance(performers);
            foreach (var artist in Artists)
            {
                var rp = new RagaPerformance
                {
                    Artist = artist,
                    Raga = Raga,
                    Performance = performance
                };
                MusicDb.RagaPerformances.Add(rp);
            }
            return new IndianClassicalRagaCatalogueResult(this, CatalogueStatus.Success, Artists, Raga,  performance);
        }

        private Raga GetRaga(string name)
        {
            Debug.Assert(MusicDb != null);
            var lowerAlphaNumericName = name.ToAlphaNumerics().ToLower();
            var raga = MusicDb.Ragas.SingleOrDefault(r => r.AlphamericName.ToLower() == lowerAlphaNumericName);
            if (raga == null)
            {
                var alphamericName = name.ToAlphaNumerics();
                raga = new Raga
                {
                    Name = name,
                    DisplayName = string.IsNullOrWhiteSpace(ici.Lookup[alphamericName].DisplayName) ? $"Raga {name}" : ici.Lookup[alphamericName].DisplayName,
                    //DisplayName = ici.Lookup[alphamericName].DisplayName ?? $"Raga {name}",
                    AlphamericName = name.ToAlphaNumerics()
                };
                MusicDb.Ragas.Add(raga);
            }
            return raga;
        }
        protected override string GetName()
        {

            return $"{artistPerformers.ToCSV((mp) => mp.Name)}:{ragaName}";
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
