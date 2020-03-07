namespace Fastnet.Music.MediaTools
{
    /// <summary>
    /// What kind of picture is in the flac file, picture type according to the ID3v2 APIC frame.
    /// </summary>
    public enum PictureType
    {
        /// <summary>
        /// A general picture.
        /// </summary>
        Other = 0,
        /// <summary>
        /// The picture is a file icon.
        /// </summary>
        FileIcon = 1,
        /// <summary>
        /// The picture is another file icon.
        /// </summary>
        OtherFileIcon = 2,
        /// <summary>
        /// The picture is the front cover of an album.
        /// </summary>
        CoverFront = 3,
        /// <summary>
        /// The picture is the back cover of an album.
        /// </summary>
        CoverBack = 4,
        /// <summary>
        /// The picture is the leaflet page of an album.
        /// </summary>
        LeafletPage = 5,
        /// <summary>
        /// The picture is a media page (e.g. label of CD).
        /// </summary>
        Media = 6,
        /// <summary>
        /// The picture of the lead artist.
        /// </summary>
        LeadArtist = 7,
        /// <summary>
        /// Picture of the artist.
        /// </summary>
        Artist = 8,
        /// <summary>
        /// Picture of the conductor.
        /// </summary>
        Conductor = 9,
        /// <summary>
        /// Picture of the band.
        /// </summary>
        Band = 10,
        /// <summary>
        /// picture of the composer.
        /// </summary>
        Composer = 11,
        /// <summary>
        /// Picture of the Lyricist.
        /// </summary>
        Lyricist = 12,
        /// <summary>
        /// Picture of the recording location.
        /// </summary>
        RecordingLocation = 13,
        /// <summary>
        /// Picture during the recording.
        /// </summary>
        DuringRecording = 14,
        /// <summary>
        /// Picture during the performance.
        /// </summary>
        DuringPerformance = 15,
        /// <summary>
        /// A movie screen capture picture.
        /// </summary>
        MovieScreenCapture = 16,
        /// <summary>
        /// A picture of a bright coloured fish. Yes, really ... a fish. Brightly coloured even!
        /// </summary>
        BrightColouredFish = 17,
        /// <summary>
        /// A picture of an illustration.
        /// </summary>
        Illustration = 18,
        /// <summary>
        /// A picture of the artist logo.
        /// </summary>
        ArtistLogotype = 19,
        /// <summary>
        /// The studio logo.
        /// </summary>
        StudioLogotype = 20
    }
}
