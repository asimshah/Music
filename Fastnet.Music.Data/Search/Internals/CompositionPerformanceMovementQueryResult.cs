namespace Fastnet.Music.Data
{
    internal class CompositionPerformanceMovementQueryResult
    {
        public SearchKey Composer { get; set; }
        public SearchKey Composition { get; set; }
        public SearchKey Performance { get; set; }
        public TrackKey Movement { get; set; }
    }
}
