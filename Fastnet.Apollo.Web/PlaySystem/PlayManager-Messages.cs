using Fastnet.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Messages;
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
        public async Task SendPlaylist(PlaylistDTO dto)
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
        public async Task SendDeviceStatus(DeviceRuntime dr)
        {
            try
            {
                await this.messageHub.Clients.All.SendDeviceStatus(dr.Status.ToDTO(dr));
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }
        }
    }
}
