using Fastnet.Music.Messages;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    public partial interface IHubMessage
    {
        Task SendArtistNewOrModified(long id);
        Task SendArtistDeleted(long id);


        Task SendDeviceNameChanged(AudioDevice d);
        Task SendDeviceEnabled(AudioDevice d);
        Task SendDeviceDisabled(AudioDevice d);
        Task SendDeviceStatus(DeviceStatusDTO d);
        Task SendPlaylist(PlaylistDTO update);
        Task SendCommand(PlayerCommand command);
    }
}
