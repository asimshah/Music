using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Fastnet.Music.Data
{
    public class Composition : EntityBase
    {
        public override long Id { get; set; }
        [Required, MaxLength(512)]
        public string Name { get; set; }
        [MaxLength(512)]
        public string AlphamericName { get; set; }
        [MaxLength(ILengthConstants.MaxCompressedNameLength)]
        public string CompressedName { get; set; }
        public long ArtistId { get; set; }
        public virtual Artist Artist { get; set; } // the composer
        //public virtual ICollection<Performance> Performances { get; } = new HashSet<Performance>(); // all the albums that contain a performance of this composition
        [NotMapped]
        public IEnumerable<Performance> Performances => CompositionPerformances.Select(x => x.Performance);
        public virtual ICollection<CompositionPerformance> CompositionPerformances { get; } = new HashSet<CompositionPerformance>();
        public override string ToString()
        {
            return $"{Artist.Name}: {Name}";
        }
    }
}
