namespace Fastnet.Music.Data
{
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
            return ToIdent();// $"[C-{CompositionId}+P-{PerformanceId}]";
        }
    }
}
