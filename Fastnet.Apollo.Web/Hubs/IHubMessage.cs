﻿using Fastnet.Music.Messages;
using System;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    public partial interface IHubMessage
    {
        Task SendArtistNewOrModified(long id);
        Task SendArtistDeleted(long id);


        Task SendDeviceNameChanged(AudioDevice d);
        Task SendDeviceEnabled(AudioDevice d);
        [Obsolete]
        Task SendDeviceDisabled(AudioDevice d);
        Task SendDeviceDisabled(string deviceKey);
        Task SendDeviceStatus(DeviceStatusDTO d);
        Task SendPlaylist(PlaylistDTO update);
        Task SendCommand(PlayerCommand command);
    }
}
