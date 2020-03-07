namespace Fastnet.Music.Core
{
    /// <summary>
    /// States that an AudioDevice can be in
    /// </summary>
    public enum PlayerStates
    {
        /// <summary>
        /// player has not started - device status broadcasts DO NOT occur
        /// </summary>
        Initial,
        /// <summary>
        /// doing nothing at all - device status broadcasts DO NOT occur
        /// </summary>
        SilentIdle,
        /// <summary>
        /// doing nothing at all - device status broadcasts occur
        /// </summary>
        Idle,
        /// <summary>
        /// Currently playing
        /// </summary>
        Playing,
        /// <summary>
        /// Currently paused by human action (would restart from the point at which playing item is paused)
        /// </summary>
        Paused,
        /// <summary>
        /// waiting next item in playlist
        /// </summary>
        WaitingNext,
        ///// <summary>
        ///// Currently stopped by human action (would restart from the beginning of the item being played)
        ///// </summary>
        //Stopped, 
        /// <summary>
        /// AudioDevice has failed in some way and cannot be used
        /// </summary>
        Fault
    }
}
