using Fastnet.Core;
using Fastnet.Music.Data;
using System;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    public partial class PlayManager
    {
        public async Task SendDeviceEnabled(Device device)
        {
            try
            {
                await this.messageHub.Clients.All.SendDeviceEnabled(device.ToDTO());
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }
        }
        public async Task SendDeviceDisabled(Device device)
        {
            try
            {
                await this.messageHub.Clients.All.SendDeviceDisabled(device.ToDTO());
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }

        }
        public async Task SendDeviceNameChanged(Device device)
        {
            try
            {
                await this.messageHub.Clients.All.SendDeviceNameChanged(device.ToDTO());
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }

        }
        public async Task SendPlaylist(PlaylistUpdateDTO dto)
        {
            try
            {
                await this.messageHub.Clients.All.SendPlaylist(dto);
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }
        }

    }
}
