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
        public override string ToString()
        {
            return $"{Composition.Name}, {Movements.Count} movements: \"{GetAllPerformersCSV()}\"";
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
        private Composition GetComposition()
        {
            if(StyleId == MusicStyles.WesternClassical)
            {
                return CompositionPerformances.Select(x => x.Composition).Single();
            }
            //else
            //{
            //    throw new Exception($"performance {ToIdent()} in {StyleId} does not have a composition");
            //}
            return null;
        }
    }
}
