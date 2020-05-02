using System.Collections.Generic;
using System.Linq;

namespace Fastnet.Music.Data
{
    internal class IndianClassicalRagaPerformanceMovementQueryResult : IndianClassicalRagaPerformanceQueryResult, IndianClassicalQueryResult
    {
        public List<TrackKey> Movements { get; } = new List<TrackKey>();
        //public IndianClassicalRagaPerformanceMovementQueryResult(RagaPerformance rp, Track track) : base(rp)
        //{
        //    //Movement = new TrackKey { Key = track.Id, Name = track.Title, Number = track.Number };
        //}
        public IndianClassicalRagaPerformanceMovementQueryResult(IEnumerable<Artist> artists, Raga raga, Performance performance, IEnumerable<Track> movements) : base(artists, raga, performance)
        {
            //Movement = new TrackKey { Key = movement.Id, Name = movement.Title, Number = movement.MovementNumber };
            Movements.AddRange(movements.Select(movement => new TrackKey { Key = movement.Id, Name = movement.Title, Number = movement.MovementNumber }));
        }
    }
}
