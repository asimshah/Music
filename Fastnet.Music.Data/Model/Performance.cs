using Fastnet.Core;
using Fastnet.Music.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Fastnet.Music.Data
{
    public class Performance : EntityBase, IPlayable
    {
        public override long Id { get; set; }
        public MusicStyles StyleId { get; set; } // helps distinguish performance between music styles - e.g WesternClassical uses Composition ->> Performances, Indian Classical uses Raga ->> Performances
        [Obsolete("retain till all databases have been upgraded")]
        public long CompositionId { get; set; }
        //public virtual Composition Composition { get; set; } // the composition performed
        public Composition Composition => GetComposition();// CompositionPerformances.Select(x => x.Composition).Single();
        public int Year { get; set; }
        public virtual ICollection<Track> Movements { get; } = new HashSet<Track>(); // the 'album' containing this performance is the Work that these tracks belong to
        [Obsolete("remove after all dbs have updated")]
        [MaxLength(2048)]
        public string Performers { get; set; } // CSV of performers
        [Obsolete("remove after all dbs have updated")]
        public string Orchestras { get; set; } // CSV of orchestras
        [Obsolete("remove after all dbs have updated")]
        public string Conductors { get; set; } // CSV of conductors
        [MaxLength(2048)]
        public string AlphamericPerformers { get; set; }
        [MaxLength(ILengthConstants.MaxCompressedNameLength)]
        public string CompressedName { get; set; }
        [NotMapped]
        IEnumerable<Track> IPlayable.Tracks => Movements;
        [NotMapped]
        string IPlayable.Name => GetAllPerformersCSV();
        public virtual List<PerformancePerformer> PerformancePerformers { get; } = new List<PerformancePerformer>();
        public virtual ICollection<CompositionPerformance> CompositionPerformances { get; } = new HashSet<CompositionPerformance>();
        public virtual ICollection<RagaPerformance> RagaPerformances { get; } = new HashSet<RagaPerformance>();
        /// <summary>
        /// returns either the composition or the raga name as appropriate
        /// </summary>
        /// <returns></returns>
        public string GetParentEntityName(bool compressed = false)
        {
            if (compressed)
            {
                return GetComposition()?.CompressedName ?? GetRaga().CompressedName;
            }
            else
            {
                return GetComposition()?.Name ?? $"{GetRaga().Name}";
            }
        }
        public string GetParentEntityDisplayName()
        {
            return GetComposition()?.Name ?? $"{GetRaga().DisplayName}";
        }
        /// <summary>
        /// returns a csv string containing the artist(s) for this composition or raga
        /// </summary>
        /// <returns></returns>
        public string GetParentArtistsName(bool compressed = false)
        {
            if (compressed)
            {
                return GetComposition()?.Artist.CompressedName ?? string.Join(string.Empty, RagaPerformances.Select(x => x.Artist.CompressedName));
            }
            else
            {
                return GetComposition()?.Artist.Name ?? RagaPerformances.Select(x => x.Artist.Name).ToCSV();
            }
        }
        public override string ToString()
        {
            return $"{GetParentEntityName()}, {Movements.Count} movements: \"{GetAllPerformersCSV()}\"";
        }
        public string ToLogIdentity()
        {
            object pe = GetComposition() as object ?? GetRaga() as object;
            var text = "";
            switch(pe)
            {
                case Composition c:
                    text = $"[A-{c.Artist.Id}] {c.Artist.Name}, [C-{c.Id}] {c.Name}, [P-{Id}]";
                    break;
                case Raga r:
                    var artists = RagaPerformances.Select(x => x.Artist);
                    text = $"[A-{artists.Select(a => a.Id.ToString()).ToCSV()}] {artists.Select(a => a.Name.ToString()).ToCSV()}, [R-{r.Id}] {r.Name}, [P-{Id}]";
                    break;
            }
            return text;
            //log.Information($"[A-{performance.Composition.Artist.Id}] {performance.Composition.Artist.Name}, [C-{performance.Composition.Id}] {performance.Composition.Name}, [P-{performance.Id}] {performance.GetAllPerformersCSV()} reset");
        }
        public string GetOrchestrasCSV(bool includeAll = false)
        {
            return GetPerformerCSV(PerformerType.Orchestra, includeAll);
        }
        public string GetConductorsCSV(bool includeAll = false)
        {
            return GetPerformerCSV(PerformerType.Conductor, includeAll);
        }
        public string GetOtherPerformersCSV(bool includeAll = false)
        {
            return GetPerformerCSV(PerformerType.Other, includeAll);
        }
        public string GetAllPerformersCSV(bool includeAll = false)
        {
            return GetPerformerCSV(null, includeAll);
        }
        public IEnumerable<PerformancePerformer> GetPerformancePerformerSubSet(PerformerType? type = null)
        {
            return PerformancePerformers
                .Where(pp => type == null || pp.Performer.Type == type);
        }
        private string GetPerformerCSV(PerformerType? type = null, bool includeAll = false)
        {
            var r = GetPerformancePerformerSubSet(type)
                .Where(pp => (pp.Selected || includeAll == true))
                .OrderBy(pp => pp.Performer.Type)
                .ThenBy(x => x.Performer.Name.GetLastName())
                .Select(pp => pp.Performer.Name);
            return string.Join(", ", r);
        }
        public Composition GetComposition()
        {
            if(StyleId == MusicStyles.WesternClassical)
            {
                return CompositionPerformances.Select(x => x.Composition).Single();
            }
            return null;
        }
        public Raga GetRaga()
        {
            if (StyleId == MusicStyles.IndianClassical)
            {
                return RagaPerformances.Select(x => x.Raga).Distinct().Single();
            }
            return null;
        }
    }
}
