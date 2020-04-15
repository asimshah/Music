using System.Collections.Generic;

namespace Fastnet.Music.Data
{
    public interface IPlayable
    {
        long Id { get; }
        string Name { get; }
        IEnumerable<Track> Tracks { get; }
    }
}
