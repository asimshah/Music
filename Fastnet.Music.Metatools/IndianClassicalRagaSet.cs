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
    public class IndianClassicalRagaSet : MusicSetWithPerformances
    {
        //private Raga Raga { get; set; }
        private readonly List<MetaPerformer> otherPerformers = new List<MetaPerformer>();
        private readonly List<MetaPerformer> artistPerformers = new List<MetaPerformer>();
        private readonly string ragaName;

        internal IndianClassicalRagaSet(MusicDb db, MusicOptions musicOptions,
            string ragaName, IEnumerable<MetaPerformer> artists, IEnumerable<MetaPerformer> otherPerformers,
            IEnumerable<MusicFile> musicFiles, TaskItem taskItem)
            : base(db, musicOptions, MusicStyles.IndianClassical, musicFiles, taskItem)
        {
            this.artistPerformers.AddRange(artists);
            this.otherPerformers.AddRange(otherPerformers);// allPerformers.Where(x => x.type != PerformerType.Artist).ToList();
            this.ragaName = ragaName;
            //this.year = musicFiles.Select(f => f.GetYear() ?? 0).Max();
        }
        public async override Task<CatalogueResultBase> CatalogueAsync()
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
                raga = new Raga
                {
                    Name = name,
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
