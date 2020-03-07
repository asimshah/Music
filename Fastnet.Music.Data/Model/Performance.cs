using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fastnet.Music.Data
{
    public interface IPlayable
    {
        long Id { get; }
        string Name { get; }
        IEnumerable<Track> Tracks { get; }
    }
    public class Performance : IPlayable
    {
        public long Id { get; set; }
        public long CompositionId { get; set; }
        public virtual Composition Composition { get; set; } // the composition performed
        public int Year { get; set; }
        public virtual ICollection<Track> Movements { get; } = new HashSet<Track>(); // the 'album' containing this performance is the Work that these tracks belong to
        [MaxLength(2048)]
        public string Performers { get; set; } // CSV of performers
        public string Orchestras { get; set; } // CSV of orchestras
        public string Conductors { get; set; } // CSV of conductors
        [MaxLength(2048)]
        public string AlphamericPerformers { get; set; }
        [MaxLength(16)]
        public string CompressedName { get; set; }
        [NotMapped]
        IEnumerable<Track> IPlayable.Tracks => Movements;
        [NotMapped]
        string IPlayable.Name => Performers;
        public override string ToString()
        {
            return $"{Composition.Name}, {Movements.Count} movements: \"{Performers}\"";
        }
    }
}
