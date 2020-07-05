using Newtonsoft.Json;

namespace Fastnet.Apollo.Web
{
    public class PlaylistPosition
    {

        public int Major { get; private set; } = 0;

        public int Minor { get; private set; } = 0;
        public PlaylistPosition()
        {
            //Reset();
        }
        public PlaylistPosition(int major, int minor)
        {
            Major = major;
            Minor = minor;
        }
        public void Set(PlaylistPosition position)
        {
            //this.Major = position.Major;
            //this.Minor = position.Minor;
        }
        //public void Reset()
        //{
        //    this.Major = 0;
        //    this.Minor = 0;
        //}
        public bool IsZero()
        {
            return Major == 0 && Minor == 0;
        }
        public PlaylistPosition GetNextMajor()
        {
            return new PlaylistPosition(Major + 1, 0);
        }
        public PlaylistPosition GetNextMinor()
        {
            return new PlaylistPosition(Major, Minor + 1);
        }
        public PlaylistPosition GetPreviousMajor()
        {
            return new PlaylistPosition(Major - 1, 0);
        }
        public PlaylistPosition GetPreviousMinor()
        {
            return new PlaylistPosition(Major, Minor - 1);
        }
        public static PlaylistPosition ZeroPosition => new PlaylistPosition(0, 0);
        public override string ToString()
        {
            return $"({Major}, {Minor})";
        }
    }
}
