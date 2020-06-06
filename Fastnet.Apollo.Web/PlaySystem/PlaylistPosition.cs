namespace Fastnet.Apollo.Web
{
    public class PlaylistPosition
    {
        public int Major { get; private set; }
        public int Minor { get; private set; }
        public PlaylistPosition()
        {
            Reset();
        }
        public PlaylistPosition(int major, int minor)
        {
            Major = major;
            Minor = minor;
        }
        public void Set(PlaylistPosition position)
        {
            this.Major = position.Major;
            this.Minor = position.Minor;
        }
        public void Reset()
        {
            this.Major = 0;
            this.Minor = 0;
        }
        public bool IsUnset()
        {
            return Major == 0 && Minor == 0;
        }
        public override string ToString()
        {
            return $"({Major}, {Minor})";
        }
    }
}
