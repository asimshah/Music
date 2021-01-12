using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Messages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    public class DeviceService : HostedService
    {
        #region Private Fields
        private readonly ILoggerFactory lf;
        private readonly IHubContext<MessageHub, IHubMessage> messageHub;
        private readonly Messenger messenger;
        private readonly IOptionsMonitor<MusicServerOptions> musicServerOptionsMonitor;
        private readonly ConcurrentDictionary<string, DeviceRuntime> runtimeDevices;
        private readonly MusicServerInformation serverInformation;
        private CancellationToken cancellationToken;

        #endregion Private Fields

        #region Public Constructors

        public DeviceService(IHubContext<MessageHub, IHubMessage> messageHub,
            IOptionsMonitor<MusicServerOptions> musicServerOptionsMonitor,
            ILoggerFactory lf,
            Messenger messenger,
            ILogger<DeviceService> logger) : base(logger)
        {
            this.messageHub = messageHub;
            this.musicServerOptionsMonitor = musicServerOptionsMonitor;
            this.runtimeDevices = new ConcurrentDictionary<string, DeviceRuntime>();
            this.lf = lf;
            this.messenger = messenger;
            if (!this.messenger.MulticastEnabled)
            {
                this.messenger.EnableMulticastSend();
            }
            serverInformation = GetMusicServerInformation();
        }

        #endregion Public Constructors

        #region Public Methods

        #region device-methods
        public async Task AddDevice(AudioDevice audioDevice, ExtendedPlaylist epl)
        {
            if (!runtimeDevices.ContainsKey(audioDevice.Key))
            {
                var dr = new DeviceRuntime
                {
                    Key = audioDevice.Key,
                    Type = audioDevice.Type,
                    DisplayName = audioDevice.DisplayName,
                    MaxSampleRate = audioDevice.Capability.MaxSampleRate,
                    PlayerUrl = audioDevice.Url,
                    CommandSequenceNumber = 0,
                    Status = new DeviceStatus
                    {
                        Key = audioDevice.Key,
                        State = PlayerStates.Idle
                    }
                };
                dr.SetPlaylist(epl);
                if (this.runtimeDevices.TryAdd(dr.Key, dr))
                {

                    log.Information($"{audioDevice}, {audioDevice.Url} added to run time");
                    await PlayerResetAsync(dr.Key);
                    await this.SendDeviceEnabled(audioDevice);
                    //await this.SetPlaylistAsync(dr, device.Playlist);
                    /*******TODO  update keepalive player urls**************/
                }
                else
                {
                    log.Error($"failed to add device {audioDevice.Key} to run time");
                }
            }
            else
            {
                //log.Warning($"{audioDevice.Key} already present in run time");
            }
        }
        public async Task BrowserDisconnectedAsync(string browserKey)
        {
            RemoveDeviceFromRuntime(browserKey);
            await this.SendDeviceDisabled(browserKey);
        }
        public IEnumerable<DeviceRuntime> GetDeviceRuntimes()
        {
            return this.runtimeDevices.Values.ToArray();
        }
        public DeviceStatusDTO GetDeviceStatus(string deviceKey)
        {
            var dr = GetDeviceRuntime(deviceKey);
            return dr.Status.ToDTO(dr);
        }
        public bool IsPresentInRuntime(string deviceKey)
        {
            var dr = GetDeviceRuntime(deviceKey);
            return dr != null;
        }
        public async Task SendDeviceStatus(DeviceStatus deviceStatus)
        {
            await this.OnDeviceStatusAsync(deviceStatus);
        }
        public async Task UpdateDeviceRuntimeAsync(AudioDevice ad, bool isDisabled)
        {
            var dr = GetDeviceRuntime(ad.Key);
            if (dr != null)
            {
                // device is present in the run time
                if (isDisabled)
                {
                    RemoveDeviceFromRuntime(ad.Key);
                    await this.SendDeviceDisabled(ad.Key);
                }
                else
                {
                    if (dr.DisplayName != ad.DisplayName)
                    {
                        var oldName = dr.DisplayName;
                        dr.DisplayName = ad.DisplayName;
                        //await SendDeviceNameChanged(device);
                        await this.SendDeviceNameChanged(ad);
                        log.Information($"Device {oldName} named changed to {ad.DisplayName}");
                    }
                    // resync with the clients, just in case ...
                    await this.SendDeviceEnabled(ad);
                }
            }
            else
            {
                // device is not present in the run time
                //if (!isDisabled)
                //{
                //    await this.AddDevice(ad);
                //    await this.SendDeviceEnabled(ad);
                //}
            }
            log.Debug($"devices runtime contains {this.runtimeDevices.Count()} device");
        }
        #endregion device-methods

        #region playlist-methods

        public async Task AddExtendedPlaylistItem(string deviceKey, ExtendedPlaylistItem epli)
        {
            var dr = GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                dr.ExtendedPlaylist.AddItem(epli);
                await SendPlaylist(dr.ToPlaylistDTO());
            }
        }
        public IEnumerable<string> FindDevicesUsingPlaylist(long playlistId)
        {
            return this.runtimeDevices.Values
                .Where(x => x.ExtendedPlaylist.PlaylistId == playlistId)
                .Select(x => x.Key);
        }
        public ExtendedPlaylist GetExtendedPlaylist(string deviceKey)
        {
            var dr = GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                return dr.ExtendedPlaylist;
            }
            return null;
        }
        public async Task SetExtendedPlaylist(string deviceKey, ExtendedPlaylist epl)
        {
            var dr = GetDeviceRuntime(deviceKey);
            if(dr != null)
            {
                dr.SetPlaylist(epl);
                await SendPlaylist(dr.ToPlaylistDTO());
            }
        }
        #endregion playlist-methods

        #region player-commands
        public async Task<int> PlayNextAsync(string deviceKey, bool reverse = false)
        {
            var dr = GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                var nextItem = reverse ? dr.GetPreviousItem() : dr.GetNextItem();
                if (nextItem != null)
                {
                    PlayerCommand pc = null;
                    pc = new PlayerCommand
                    {
                        Command = PlayerCommands.Play,
                        Volume = dr.Status.Volume < 0.05f ? 0.3f : dr.Status.Volume,
                        DeviceKey = deviceKey,
                        StreamUrl = $"player/stream/{nextItem.MusicFileId}"
                    };
                    //dr.CurrentPosition.Set(position);
                    return await ExecuteCommandAsync(dr, pc);
                }
            }
            return 0;
        }
        public async Task<int> PlaySequenceNumberAsync(string deviceKey, PlaylistPosition position)
        {
            var dr = GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                var stpli = dr.GetItemAtPosition(position);
                if (stpli != null)
                {
                    var pc = new PlayerCommand
                    {
                        Command = PlayerCommands.Play,
                        Volume = dr.Status.Volume < 0.05f ? 0.3f : dr.Status.Volume,
                        DeviceKey = deviceKey,
                        StreamUrl = $"player/stream/{stpli.MusicFileId}"
                    };
                    //dr.CurrentPosition.Set(position);
                    dr.CurrentPosition = position;
                    return await ExecuteCommandAsync(dr, pc);
                }
                else
                {
                    log.Warning($"Device {dr.DisplayName} no playlist item at sequence {position}");
                    dr.CommandSequenceNumber = (dr.CommandSequenceNumber + 1) % 1024;
                }
            }
            return dr.CommandSequenceNumber;// 0;
        }
        public async Task<int> SetPositionAsync(string deviceKey, float position)
        {
            var pc = new PlayerCommand
            {
                Command = PlayerCommands.SetPosition, //.JumpTo,
                DeviceKey = deviceKey,
                Position = position
            };
            return await ExecuteCommandAsync(deviceKey, pc);
        }
        public async Task<int> SetVolumeAsync(string deviceKey, float volume)
        {
            var pc = new PlayerCommand
            {
                Command = PlayerCommands.SetVolume,
                DeviceKey = deviceKey,
                Volume = volume
            };
            return await ExecuteCommandAsync(deviceKey, pc);
        }
        public async Task<int> TogglePlayPauseAsync(string deviceKey)
        {
            var pc = new PlayerCommand
            {
                Command = PlayerCommands.TogglePlayPause,
                DeviceKey = deviceKey,
            };
            pc.JsonParcel = GetDeviceRuntime(deviceKey)?.MostRecentCommand?.JsonParcel;
            return await ExecuteCommandAsync(deviceKey, pc);
        }
        #endregion player-commands

        #region messages-to-hub-clients
        public async Task SendDeviceStatus(string deviceKey)
        {
            var dr = GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                await SendDeviceStatus(dr.Status.ToDTO(dr));
            }
        }
        public async Task SendPlaylist(string deviceKey)
        {
            var dr = GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                await SendPlaylist(dr.ToPlaylistDTO());
            }
        }
        //public async Task SendDeviceDisabled(AudioDevice audioDevice)

        #endregion messages-to-hub-clients

        #endregion Public Methods

        #region Protected Methods

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            log.Information($"started");
            this.cancellationToken = cancellationToken;
            messenger.AddMulticastSubscription<DeviceStatus>(async (m) => { await OnDeviceStatusAsync(m); });
            while (!this.cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await this.messenger.SendMulticastAsync(this.serverInformation);
                    var interval = musicServerOptionsMonitor.CurrentValue.Intervals.ServerInformationBroadcastInterval;
                    await Task.Delay(interval, this.cancellationToken);
                }
                catch (Exception xe)
                {
                    log.Error(xe);
                }
            }
            log.Information($"CancellationRequested");
        }

        #endregion Protected Methods

        #region Private Methods

        #region messages-to-hub-clients
        private async Task SendDeviceDisabled(string deviceKey)
        {
            try
            {
                await this.messageHub.Clients.All.SendDeviceDisabled(deviceKey);
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }
        }
        private async Task SendDeviceEnabled(AudioDevice audioDevice)
        {
            try
            {
                await this.messageHub.Clients.All.SendDeviceEnabled(audioDevice);
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }
        }
        private async Task SendDeviceNameChanged(AudioDevice audioDevice)
        {
            try
            {
                await this.messageHub.Clients.All.SendDeviceNameChanged(audioDevice);
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }
        }
        private async Task SendDeviceStatus(DeviceStatusDTO deviceStatus)
        {
            try
            {
                await this.messageHub.Clients.All.SendDeviceStatus(deviceStatus);
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }
        }
        private async Task SendPlaylist(PlaylistDTO dto)
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
        private async Task SendWebAudioCommand(PlayerCommand command)
        {
            try
            {
                await this.messageHub.Clients.Group("WebAudio").SendCommand(command);
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }
        }
        #endregion  messages-to-hub-clients


        private async Task<int> ExecuteCommandAsync(string key, PlayerCommand playerCommand/*, Action<DeviceRuntime> afterExecute = null*/)
        {
            var dr = GetDeviceRuntime(key);
            return await ExecuteCommandAsync(dr, playerCommand/*, afterExecute*/);
        }
        private async Task<int> ExecuteCommandAsync(DeviceRuntime dr, PlayerCommand playerCommand/*, Action<DeviceRuntime> afterExecute = null*/)
        {
            PlayerClient GetPlayerClient(DeviceRuntime deviceRuntime)
            {
                return new PlayerClient(deviceRuntime.PlayerUrl, lf.CreateLogger<PlayerClient>());
            }
            try
            {
                if (dr != null)
                {
                    dr.CommandSequenceNumber = (dr.CommandSequenceNumber + 1) % 1024;
                    dr.MostRecentCommand = playerCommand;
                    playerCommand.CommandSequenceNumber = dr.CommandSequenceNumber;
                    if (dr.Type != AudioDeviceType.Browser)
                    {
                        using (var deviceClient = GetPlayerClient(dr))
                        {
                            await deviceClient.Execute(playerCommand);
                        }
                    }
                    else
                    {
                        await this.SendWebAudioCommand(playerCommand);
                        //await this.messageHub.Clients.Group("WebAudio").SendCommand(playerCommand);
                    }
                    log.Debug($"{dr.DisplayName}: command {playerCommand.Command}, command sequence {playerCommand.CommandSequenceNumber}");
                    return playerCommand.CommandSequenceNumber;
                }
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }
            return 0;
        }
        private DeviceRuntime GetDeviceRuntime(string key)
        {
            if (runtimeDevices.ContainsKey(key))
            {
                return runtimeDevices[key];
            }
            else
            {
                // this can occur normally when starting up device has not yet been set up
                log.Debug($"device {key} not found in run time");
            }
            return null;
        }

        private MusicServerInformation GetMusicServerInformation()
        {
            var ipAddress = NetInfo.GetLocalIPAddress();
            return new MusicServerInformation
            {
                MachineName = Environment.MachineName.ToLower(),
                ProcessId = System.Diagnostics.Process.GetCurrentProcess().Id,
                Url = $"http://{ipAddress.ToString()}:{musicServerOptionsMonitor.CurrentValue.Port}"
            };
        }
        private async Task OnDeviceStatusAsync(DeviceStatus deviceStatus)
        {
            try
            {
                var dr = GetDeviceRuntime(deviceStatus.Key);
                if (dr != null)
                {
                    try
                    {
                        dr.Status = deviceStatus;
                        await this.SendDeviceStatus(dr.Status.ToDTO(dr));
                    }
                    catch (Exception xe)
                    {
                        log.Error(xe);
                    }
                }
                else
                {
                    log.Warning($"Update received from device {deviceStatus.Key} [{deviceStatus.MachineName}], state {deviceStatus.State} - not found in device run time");
                }
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }
        }
        private async Task<int> PlayerResetAsync(string deviceKey)
        {
            var pc = new PlayerCommand
            {
                Command = PlayerCommands.Reset,
                DeviceKey = deviceKey,
            };
            return await ExecuteCommandAsync(deviceKey, pc);
        }
        private void RemoveDeviceFromRuntime(string deviceKey)
        {
            if (this.runtimeDevices.TryRemove(deviceKey, out _))
            {
                //this.keepAlive.SetPlayerUrls(this.GetPlayerUrls());
                log.Information($"Device {deviceKey} removed from run time");
            }
        }
        #endregion Private Methods
    }
}
