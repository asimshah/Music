namespace Fastnet.Music.Data
{
    public class RagaPerformance
    {
        public long PerformanceId { get; set; } 
        public virtual Performance Performance { get; set; }
        public long RagaId { get; set; }
        public virtual Raga Raga { get; set; }
        public long ArtistId { get; set; }
        public virtual Artist Artist { get; set; }
        public string ToIdent()
        {
            return $"{Raga.ToIdent()}{Performance.ToIdent()}{((IIdentifier) Artist).ToIdent()}".Replace("][", "+");
        }
        public override string ToString()
        {
            return ToIdent();
        }
    }
}
