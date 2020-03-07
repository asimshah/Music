namespace Fastnet.Music.Core
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Music file parsing stages in order
    /// </summary>
    public enum MusicFileParsingStage
    {
        Unknown = 0,
        Initial = 1,
        IdTagsComplete = 2,
        Catalogued = 3
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
