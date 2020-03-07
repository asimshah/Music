namespace Fastnet.Music.Core
{
    /// <summary>
    /// Various play system intervals
    /// </summary>
    public class PlayIntervals
    {
        /// <summary>
        /// Interval at which player agents broadcast information (in milliseconds)
        /// Default is 2000 milliseconds
        /// </summary>
        public int PlayerInformationBroadcastInterval { get; set; }
        /// <summary>
        /// Interval at which the music server broadcasts information  (in milliseconds)
        /// Default is 10000 milliseconds
        /// </summary>
        public int ServerInformationBroadcastInterval { get; set; }
        /// <summary>
        /// Interval at which a player updates the list of local devices
        /// Default is 15000 milliseconds
        /// </summary>
        public int LocalDeviceUpdateInterval { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public PlayIntervals()
        {
            PlayerInformationBroadcastInterval = 2000;
            ServerInformationBroadcastInterval = 10000;
            LocalDeviceUpdateInterval = 15000;
        }
    }
}
