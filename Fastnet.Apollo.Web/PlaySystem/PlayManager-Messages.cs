using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Messages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        public IEnumerable<DeviceRuntime> GetDeviceRuntimes()
        {
            return this.devices.Values.ToArray();
        }
        public async Task UpdateDeviceRuntime(Device device)
        {
            var dr = GetDeviceRuntime(device.KeyName);
            if (dr != null)
            {
                // device is present in the run time
                if (device.IsDisabled)
                {
                    RemoveDeviceFromRuntime(device);
                    await SendDeviceDisabled(device);
                }
                else
                {
                    if (dr.DisplayName != device.DisplayName)
                    {
                        var oldName = dr.DisplayName;
                        dr.DisplayName = device.DisplayName;
                        await SendDeviceNameChanged(device);
                        log.Information($"Device {oldName} named changed to {device.DisplayName}");
                    }
                    // resync with the clients, just in case ...
                    await SendDeviceEnabled(device);
                }
            }
            else
            {
                // device is not present in the run time
                if (!device.IsDisabled/* && device.IsAvailable*/)
                {
                    await AddDeviceToRuntime(device);
                    await SendDeviceEnabled(device);
                }
            }
            log.Debug($"devices runtime contains {this.devices.Count()} device");
        }
        public async Task<int> PlaySequenceNumber(string deviceKey, PlaylistPosition position)
        {
            var dr = GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                //var pli = GetPlaylistItemAtSequence(dr, sequenceNumber);
                var pli = GetPlaylistItemAtSequence(dr, position);
                if (pli != null)
                {
                    var pc = new PlayerCommand
                    {
                        Command = PlayerCommands.Play, //.ClearThenPlay,
                        Volume = dr.Status.Volume < 0.05f ? 0.3f : dr.Status.Volume,
                        DeviceKey = deviceKey,
                        StreamUrl = $"player/stream/{pli.MusicFileId}"

                    };
                    //dr.CurrentPlaylistSequenceNumber = pli.Sequence;
                    dr.CurrentPosition.Set(position);
                    return await ExecuteCommand(dr, pc);
                }
                else
                {
                    log.Warning($"Device {dr.DisplayName} no playlist item at sequence {position}");
                    dr.CommandSequenceNumber = (dr.CommandSequenceNumber + 1) % 1024;
                }
            }
            return dr.CommandSequenceNumber;// 0;
        }
        public async Task<int> PlayNext(string deviceKey, bool reverse = false)
        {
            var dr = GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                (PlaylistItemRuntime pli, PlaylistPosition position) = (null, null);
                do
                {
                    (pli, position) = GetNextPlaylistItem(dr, reverse);
                    if (pli != null && dr.CanPlay(pli) == false)
                    {
                        dr.CurrentPosition.Set(position);
                    }
                } while (pli != null && dr.CanPlay(pli) == false);

                if (pli != null)
                {
                    PlayerCommand pc = null;
                    pc = new PlayerCommand
                    {
                        Command = PlayerCommands.Play,
                        Volume = dr.Status.Volume < 0.05f ? 0.3f : dr.Status.Volume,
                        DeviceKey = deviceKey,
                        StreamUrl = $"player/stream/{pli.MusicFileId}"

                    };
                    dr.CurrentPosition.Set(position);
                    return await ExecuteCommand(dr, pc);
                }
                else
                {
                    await PlaylistFinished(deviceKey);
                    dr.CurrentPosition.Reset();
                }
            }
            return 0;
        }
        public async Task<int> TogglePlayPause(string deviceKey)
        {
            var pc = new PlayerCommand
            {
                Command = PlayerCommands.TogglePlayPause,
                DeviceKey = deviceKey,

            };
            pc.JsonParcel = GetDeviceRuntime(deviceKey)?.MostRecentCommand?.JsonParcel;
            return await ExecuteCommand(deviceKey, pc);
            //return await ExecuteCommand(deviceKey, pc, (dr) => dr.IsPaused = !dr.IsPaused);
        }
        public async Task<int> SetVolume(string deviceKey, float volume)
        {
            var pc = new PlayerCommand
            {
                Command = PlayerCommands.SetVolume,
                DeviceKey = deviceKey,
                Volume = volume
            };
            return await ExecuteCommand(deviceKey, pc);
        }
        public async Task<int> SetPosition(string deviceKey, float position)
        {
            var pc = new PlayerCommand
            {
                Command = PlayerCommands.SetPosition, //.JumpTo,
                DeviceKey = deviceKey,
                Position = position
            };
            return await ExecuteCommand(deviceKey, pc);
        }
        public DeviceRuntime GetDeviceRuntime(string key)
        {
            if (devices.ContainsKey(key))
            {
                return devices[key];
            }
            return null;
        }
        public async Task ClearPlaylist(string deviceKey)
        {
            var dr = GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                dr.Playlist.Items.Clear();
                dr.CurrentPosition.Reset();
                var dto = new PlaylistUpdateDTO
                {
                    DeviceKey = deviceKey,
                    DisplayName = dr.DisplayName,
                    Items = dr.Playlist.Items.Select(x => x.ToDTO())
                };
                await SendPlaylist(dto);
            }
        }
        public async Task BrowserDisconnected(string browserKey)
        {
            using (var scope = new ScopedDbContext<MusicDb>(serviceProvider))
            {
                var device = scope.Db.Devices.SingleOrDefault(x => x.KeyName == browserKey);
                if (device != null)
                {
                    RemoveDeviceFromRuntime(device);
                    await SendDeviceDisabled(device);
                }
                else
                {
                    log.Warning($"audio for device {browserKey} not running");
                }
            }
        }
        public async Task AddPlaylistItem(string deviceKey, PlaylistItem pli)
        {
            using (var scope = new ScopedDbContext<MusicDb>(serviceProvider))
            {
                var dr = GetDeviceRuntime(deviceKey);
                if (dr != null)
                {
                    var item = pli.ToRuntime(scope.Db, dr);
                    if (item != null)
                    {
                        dr.Playlist.Items.Add(pli.ToRuntime(scope.Db, dr));
                        var dto = new PlaylistUpdateDTO
                        {
                            DeviceKey = deviceKey,
                            Items = dr.Playlist.Items.Select(x => x.ToDTO())
                        };
                        await SendPlaylist(dto);
                    }
                }
            }
        }
        private async Task<int> PlayerReset(string deviceKey)
        {
            var pc = new PlayerCommand
            {
                Command = PlayerCommands.Reset,
                DeviceKey = deviceKey,
            };
            return await ExecuteCommand(deviceKey, pc);
        }
        private async Task<int> PlaylistFinished(string deviceKey)
        {
            var pc = new PlayerCommand
            {
                Command = PlayerCommands.ListFinished,
                DeviceKey = deviceKey,
            };
            return await ExecuteCommand(deviceKey, pc);
        }
        private PlaylistItemRuntime GetPlaylistItemAtSequence(DeviceRuntime dr, PlaylistPosition position)
        {
            //var pli = dr.Playlist.Items.SingleOrDefault(x => x.Sequence == index);
            var pli = dr.Playlist.Items.SingleOrDefault(x => x.Sequence == position.Major);
            if (pli != null && pli.Type == PlaylistRuntimeItemType.MultipleItems)
            {
                if (position.Minor == 0)
                {
                    log.Error($"Cannot request a position with Minor == 0 when the playlist item is of type PlaylistRuntimeItemType.MultipleItems");
                    return null;
                }
                else
                {
                    pli = pli.SubItems.SingleOrDefault(x => x.Sequence == position.Minor);
                }
            }
            return pli;
        }
        private (PlaylistItemRuntime item, PlaylistPosition position) GetNextMinorItem(DeviceRuntime dr, bool reverse)
        {
            var sequence = dr.CurrentPosition.Minor + (reverse ? -1 : 1);
            var majorPli = dr.Playlist.Items.Single(x => x.Sequence == dr.CurrentPosition.Major);
            var nextMinorPli = majorPli.SubItems.SingleOrDefault(x => x.Sequence == sequence);
            return (nextMinorPli, new PlaylistPosition(dr.CurrentPosition.Major, sequence));
        }
        private (PlaylistItemRuntime item, PlaylistPosition position) GetNextMajorItem(DeviceRuntime dr, bool reverse)
        {
            var sequence = dr.CurrentPosition.Major + (reverse ? -1 : 1);
            var majorPli = dr.Playlist.Items.SingleOrDefault(x => x.Sequence == sequence);
            if (majorPli != null)
            {
                if (majorPli.Type == PlaylistRuntimeItemType.MultipleItems)
                {
                    // next major item contains minor items
                    var nextMinorPli = majorPli.SubItems.Single(x => x.Sequence == 1);
                    return (nextMinorPli, new PlaylistPosition(sequence, 1));
                }
                else
                {
                    return (majorPli, new PlaylistPosition(sequence, 0));
                }
            }
            else
            {
                return (null, new PlaylistPosition(sequence, 0));
            }
        }
        private (PlaylistItemRuntime item, PlaylistPosition position) GetNextPlaylistItem(DeviceRuntime dr, bool reverse)
        {
            try
            {
                if (!dr.CurrentPosition.IsUnset())
                {
                    var majorPli = dr.Playlist.Items.Single(x => x.Sequence == dr.CurrentPosition.Major);
                    if (majorPli.Type == PlaylistRuntimeItemType.MultipleItems)
                    {
                        (var item, var position) = GetNextMinorItem(dr, reverse);
                        if (item != null)
                        {
                            // we have a minor item
                            return (item, position);
                        }
                    }
                }
                return GetNextMajorItem(dr, reverse);
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }
            return (null, null);
        }
        public async Task OnDeviceStatus(DeviceStatus deviceStatus)
        {
            try
            {
                var dr = GetDeviceRuntime(deviceStatus.Key);
                if (dr != null)
                {
                    try
                    {
                        dr.Status = deviceStatus;
                        var dto = deviceStatus.ToDTO(dr);
                        await this.messageHub.Clients.All.SendDeviceStatus(dto);
                        log.Debug($"{dr.DisplayName}, {deviceStatus.ToString()}");
                    }
                    catch (Exception xe)
                    {
                        log.Error(xe);
                    }
                }
                else
                {
                    log.Debug($"Update received from device {deviceStatus.Key}, state {deviceStatus.State} - not found in device run time");
                }
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }
        }
        private async Task<int> ExecuteCommand(string key, PlayerCommand playerCommand/*, Action<DeviceRuntime> afterExecute = null*/)
        {
            var dr = GetDeviceRuntime(key);
            return await ExecuteCommand(dr, playerCommand/*, afterExecute*/);
        }
        private async Task<int> ExecuteCommand(DeviceRuntime dr, PlayerCommand playerCommand/*, Action<DeviceRuntime> afterExecute = null*/)
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
                        await this.messageHub.Clients.Group("WebAudio").SendCommand(playerCommand);
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
        private void RemoveDeviceFromRuntime(Device device)
        {
            lock (devices)
            {
                this.devices.Remove(device.KeyName);
            }
            //await SendDeviceDisabled(device);
            this.keepAlive.SetPlayerUrls(this.GetPlayerUrls());
            log.Information($"Device {device.DisplayName} removed from run time");
            //LogDevices();
        }
        private async Task AddDeviceToRuntime(Device device)
        {
            if (this.keepAlive != null)
            {
                using (var scope = new ScopedDbContext<MusicDb>(serviceProvider))
                {
                    DeviceRuntime dr = null;
                    lock (this.devices)
                    {
                        //if (!this.devices.ContainsKey(device.KeyName))
                        if (GetDeviceRuntime(device.KeyName) == null)
                        {
                            dr = new DeviceRuntime
                            {
                                Key = device.KeyName,
                                Type = device.Type,
                                DisplayName = device.DisplayName,
                                MaxSampleRate = device.MaxSampleRate,
                                PlayerUrl = device.PlayerUrl,
                                CommandSequenceNumber = 0,
                                Status = new DeviceStatus
                                {
                                    Key = device.KeyName,
                                    State = PlayerStates.Idle
                                }
                            };
                            dr.Playlist = device.Playlist.ToRuntime(scope.Db, dr);
                            this.devices.Add(dr.Key, dr);
                            log.Information($"{device.KeyName}, {device.DisplayName}, {device.Type}, url = {device.PlayerUrl } added to run time");
                        }
                        else
                        {
                            log.Warning($"Device {device.DisplayName} already present in run time");
                        }
                    }
                    //LogDevices();
                    if (dr != null)
                    {
                        await PlayerReset(dr.Key);
                    }
                }
                this.keepAlive.SetPlayerUrls(this.GetPlayerUrls());
            }
            else
            {
                log.Warning($"not ready to add a device - keepAlive not yet started");
            }
        }
        private void LogDevices()
        {
            lock (devices)
            {
                log.Information($"runtime is {this.devices.Count} device(s):");
                foreach (var item in this.devices)
                {
                    var dr = item.Value;
                    log.Information($"    {dr.Key}, {dr.DisplayName}, type {dr.Type.ToString()}");
                }
            }
        }
        private string[] GetPlayerUrls()
        {
            using (var scope = new ScopedDbContext<MusicDb>(serviceProvider))
            {
                var urls = scope.Db.Devices.Where(x => !x.IsDisabled && x.Type != AudioDeviceType.Browser && !string.IsNullOrWhiteSpace(x.PlayerUrl))
                    .Select(x => x.PlayerUrl).Distinct();
                return urls.ToArray();
            }
        }
        private async Task PlayerDead(string url)
        {
            var affectedDevices = this.devices.Select(x => x.Value).Where(x => string.Compare(x.PlayerUrl, url, true) == 0);
            using (var scope = new ScopedDbContext<MusicDb>(serviceProvider))
            {
                foreach (var dr in affectedDevices.ToArray())
                {
                    var device = await scope.Db.Devices.SingleAsync(x => x.KeyName == dr.Key);
                    log.Information($"Device {device.DisplayName} to be removed from run time as polling has failed ... url was {url}");
                    RemoveDeviceFromRuntime(device);
                    await SendDeviceDisabled(device);

                }
            }
        }
    }
}
