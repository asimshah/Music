namespace Fastnet.Music.Core
{
    /// <summary>
    /// 
    /// </summary>
    public enum AudioDeviceType
    {
        /// <summary>
        /// 
        /// </summary>
        Unknown,
        /// <summary>
        /// enumerated using AsioOut.GetDriverNames()
        /// </summary>
        Asio,
        /// <summary>
        /// enumerated using new MMDeviceEnumerator().EnumerateAudioEndPoints()
        /// </summary>
        Wasapi,
        /// <summary>
        /// enumerated using DirectSoundOut.Devices
        /// </summary>
        DirectSoundOut,
        /// <summary>
        /// 
        /// </summary>
        Logitech,
        /// <summary>
        /// 
        /// </summary>
        Browser

    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
