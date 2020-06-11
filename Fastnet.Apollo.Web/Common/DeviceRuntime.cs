using Fastnet.Music.Core;
using Fastnet.Music.Messages;

namespace Fastnet.Apollo.Web
{
    /// <summary>
    /// run time device info - i.e. not dependent on MusicDb instance
    /// </summary>
    public class DeviceRuntime
    {
        public DeviceStatus Status { get; set; }
        public AudioDeviceType Type { get; set; }
        public string Key { get; set; }
        public string DisplayName { get; set; }
        public int MaxSampleRate { get; set; }
        public string PlayerUrl { get; set; }
        public int CommandSequenceNumber { get; set; }
        public PlaylistPosition CurrentPosition { get; private set; }
        public PlayerCommand MostRecentCommand { get; set; }
        public PlaylistRuntime Playlist { get; set; }
        public DeviceRuntime()
        {
            CurrentPosition = new PlaylistPosition();
        }
        public bool CanPlay(PlaylistItemRuntime pli)
        {
            return !pli.NotPlayableOnCurrentDevice;// !(MaxSampleRate > 0 && pli.SampleRate > MaxSampleRate);
        }
        public override string ToString()
        {
            return $"{DisplayName} (key:{Key}) [{Type}]";
        }
    }
}
