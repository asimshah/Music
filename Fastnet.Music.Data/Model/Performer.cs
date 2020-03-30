using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fastnet.Music.Data
{
    public class Performer
    {
        public long Id { get; set; }
        public PerformerType Type { get; set; }
        [MaxLength(128)]
        public string Name { get; set; }
        public virtual List<PerformancePerformer> PerformancePerformers { get; /*set;*/ } = new List<PerformancePerformer>();
        public override string ToString()
        {
            return $"{Name}({Type})";
        }
    }
}
