namespace Fastnet.Music.Data
{
    /// <summary>
    /// types of performers in ascending order of 'primariness'
    /// </summary>
    public enum PerformerType
    {
        Other,
        Orchestra,
        Conductor,
        Artist,
        Composer// used to designate the primary artist(s) for a work, composition, or raga performance
    }
}
