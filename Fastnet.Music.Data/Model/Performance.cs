using Fastnet.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Fastnet.Music.Data
{
    public interface IPlayable
    {
        long Id { get; }
        string Name { get; }
        IEnumerable<Track> Tracks { get; }
    }
    public class CompositionPerformance : IManyToManyIdentifier
    {
        public long PerformanceId { get; set; } // we'll make this a unique key so that a performance can only appear once in this table
        public virtual Performance Performance { get; set; }
        public long CompositionId { get; set; }
        public virtual Composition Composition { get; set; }
        public string ToIdent()
        {
            return IManyToManyIdentifier.ToIdent(Composition, Performance);
        }
        public override string ToString()
        {
            return $"[C-{CompositionId}+P-{PerformanceId}]";
        }
    }
    public class Performance : EntityBase, IPlayable
    {
        public override long Id { get; set; }
        public long CompositionId { get; set; }
        //public virtual Composition Composition { get; set; } // the composition performed
        public Composition Composition => CompositionPerformances.Select(x => x.Composition).Single();
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
        public virtual List<PerformancePerformer> PerformancePerformers { get; /*set;*/ } = new List<PerformancePerformer>();
        public virtual ICollection<CompositionPerformance> CompositionPerformances { get; } = new HashSet<CompositionPerformance>();

        //public virtual CompositionPerformance CompositionPerformance { get; set;}
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
        //public string ToIdent()
        //{
        //    return ((IIdentifier)this).ToIdent();
        //}
    }
}
