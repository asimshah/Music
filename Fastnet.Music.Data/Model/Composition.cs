using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fastnet.Music.Data
{
    public class Composition
    {
        public long Id { get; set; }
        [Required, MaxLength(512)]
        public string Name { get; set; }
        [MaxLength(512)]
        public string AlphamericName { get; set; }
        [MaxLength(16)]
        public string CompressedName { get; set; }
        public long ArtistId { get; set; }
        public virtual Artist Artist { get; set; } // the composer
        public virtual ICollection<Performance> Performances { get; } = new HashSet<Performance>(); // all the albums that contain a performance of this composition
        public override string ToString()
        {
            return $"{Artist.Name}: {Name}";
        }
    }
}
