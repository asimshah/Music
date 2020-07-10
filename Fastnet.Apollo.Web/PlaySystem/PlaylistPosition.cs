using Newtonsoft.Json;

namespace Fastnet.Apollo.Web
{
    public class PlaylistPosition
    {
        #region Public Properties

        public static PlaylistPosition ZeroPosition => new PlaylistPosition(0, 0);
        [JsonProperty]
        public int Major { get; private set; } = 0;
        [JsonProperty]
        public int Minor { get; private set; } = 0;

        #endregion Public Properties

        #region Public Constructors

        public PlaylistPosition()
        {
            //Reset();
        }
        public PlaylistPosition(int major, int minor)
        {
            Major = major;
            Minor = minor;
        }

        #endregion Public Constructors

        #region Public Methods
        public PlaylistPosition Clone()
        {
            return new PlaylistPosition(Major, Minor);
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
        public bool IsZero()
        {
            return Major == 0 && Minor == 0;
        }
        public void SetMajor(int number)
        {
            this.Major = number;
        }
        public override string ToString()
        {
            return $"({Major}, {Minor})";
        }

        #endregion Public Methods
    }
}
