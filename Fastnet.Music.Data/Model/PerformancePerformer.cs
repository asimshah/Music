﻿namespace Fastnet.Music.Data
{
    public class PerformancePerformer
    {
        public long PerformanceId { get; set; }
        public virtual Performance Performance { get; set; }
        public long PerformerId { get; set; }
        public virtual Performer Performer { get; set; }
        public bool Selected { get; set; } = true;
        public override string ToString()
        {
            return $"[Pf-{PerformerId}+P-{PerformanceId}]";
        }
    }
}