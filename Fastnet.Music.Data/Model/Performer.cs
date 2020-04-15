using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fastnet.Music.Data
{
    public class Performer : EntityBase
    {
        public override long Id { get; set; }
        public PerformerType Type { get; set; }
        [MaxLength(ILengthConstants.MaxArtistNameLength)]
        public string Name { get; set; }
        [MaxLength(ILengthConstants.MaxArtistNameLength)]
        public string AlphamericName { get; set; } = string.Empty;
        public virtual List<PerformancePerformer> PerformancePerformers { get; /*set;*/ } = new List<PerformancePerformer>();
        public override string ToString()
        {
            return $"{Name}({Type})";
        }
    }
}
