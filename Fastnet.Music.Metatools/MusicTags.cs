namespace Fastnet.Music.Metatools
{
    /// <summary>
    /// Music tags are kept in a local TagFile (whereas Idtags are embedded in the music file)
    /// This allows changes to survive resetiing of thew musicdb without writing to the music files themselves
    /// </summary>
    public abstract class MusicTags
    {
        //public const string TagFile = "$TagFile.json";
        public string Filename { get; set; }
        public string Album { get; set; }
        public int TrackNumber { get; set; }
        public string Title { get; set; }
        public int Year { get; set; }
    }
}
