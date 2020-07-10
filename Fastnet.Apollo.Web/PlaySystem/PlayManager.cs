using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Messages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    static class PlayManagerExtensions
    {
        public static PlaylistItem Clone(this PlaylistItem item)
        {
            return new PlaylistItem
            {
                ItemId = item.ItemId,
                MusicFileId = item.MusicFileId,
                Sequence = item.Sequence,
                Title = item.Title,
                Type = item.Type,
            };
        }
    }
    public partial class PlayManager : HostedService // RealtimeTask
    {
        private readonly ConcurrentDictionary<string, DeviceRuntime> runtimeDevices;
        private ServerInformationMulticast sim;
        private KeepAgentsAlive keepAlive;
        private readonly IHubContext<MessageHub, IHubMessage> messageHub;
        private readonly Messenger messenger;
        private readonly LibraryService libraryService;
        private readonly ILoggerFactory lf;
        private readonly List<Task> taskList;
        //private readonly IServiceProvider serviceProvider;
        private readonly MessengerOptions messengerOptions;
        private readonly MusicServerOptions musicServerOptions;
        public PlayManager(IHubContext<MessageHub, IHubMessage> messageHub,
            IWebHostEnvironment environment, IConfiguration cfg,
            IOptions<MusicServerOptions> serverOptions, Messenger messenger, IOptions<MessengerOptions> messengerOptions,
            ILogger<PlayManager> log, ILoggerFactory loggerFactory) : base(log)
        {
            this.messengerOptions = messengerOptions.Value;
            this.messageHub = messageHub;
            //this.serviceProvider = serviceProvider;
            this.musicServerOptions = serverOptions.Value;
            this.messenger = messenger;
            this.messenger.EnableMulticastSend();
            var connectionString = environment.LocaliseConnectionString(cfg.GetConnectionString("MusicDb"));
            this.libraryService = new LibraryService(serverOptions, messageHub, loggerFactory.CreateLogger<LibraryService>(), new MusicDb(connectionString));
            this.lf = loggerFactory;
            this.taskList = new List<Task>();
            this.runtimeDevices = new ConcurrentDictionary<string, DeviceRuntime>();// new Dictionary<string, DeviceRuntime>();
        }
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            messenger.AddMulticastSubscription<DeviceStatus>(async (m) => { await OnDeviceStatusAsync(m); });
            this.taskList.Add(Task.Run(() =>
            {
                this.sim = new ServerInformationMulticast(/*environment,*/ messengerOptions, musicServerOptions, messenger, cancellationToken, lf);
                sim.Start().ContinueWith(sim.OnException, TaskContinuationOptions.OnlyOnFaulted);
                this.keepAlive = new KeepAgentsAlive(musicServerOptions, cancellationToken, lf);
                this.keepAlive.Start(this.GetPlayerUrls(), async (url) =>
                {
                    await PlayerDead(url);
                }).ContinueWith(keepAlive.OnException, TaskContinuationOptions.OnlyOnFaulted);
            }, cancellationToken));
            await Task.WhenAll(taskList);
        }
        public IEnumerable<DeviceRuntime> GetDeviceRuntimes()
        {
            return this.runtimeDevices.Values.ToArray();
        }
        public async Task UpdateDeviceRuntimeAsync(Device device)
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
            log.Debug($"devices runtime contains {this.runtimeDevices.Count()} device");
        }
        public async Task<int> PlaySequenceNumberAsync(string deviceKey, PlaylistPosition position)
        {
            var dr = GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                //var pli = GetPlaylistItemAtSequence(dr, sequenceNumber);
                //var pli = GetPlaylistItemAtSequence(dr, position);
                var stpli = dr.GetItemAtPosition(position);
                if (stpli != null)
                {
                    //var stpli = pli as SingleTrackPlaylistItem;
                    var pc = new PlayerCommand
                    {
                        Command = PlayerCommands.Play, //.ClearThenPlay,
                        Volume = dr.Status.Volume < 0.05f ? 0.3f : dr.Status.Volume,
                        DeviceKey = deviceKey,
                        StreamUrl = $"player/stream/{stpli.MusicFileId}"
                    };
                    //dr.CurrentPlaylistSequenceNumber = pli.Sequence;
                    dr.CurrentPosition.Set(position);
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
        //public async Task<int> PlayNext(string deviceKey, bool reverse = false)
        //{
        //    var dr = GetDeviceRuntime(deviceKey);
        //    if (dr != null)
        //    {

        //        (ExtendedPlaylistItem pli, PlaylistPosition position) = (null, null);
        //        do
        //        {
        //            (pli, position) = GetNextPlaylistItem(dr, reverse);
        //            if (pli != null && dr.CanPlay(pli) == false)
        //            {
        //                dr.CurrentPosition.Set(position);
        //            }
        //        } while (pli != null && dr.CanPlay(pli) == false);

        //        if (pli != null)
        //        {

        //            PlayerCommand pc = null;
        //            pc = new PlayerCommand
        //            {
        //                Command = PlayerCommands.Play,
        //                Volume = dr.Status.Volume < 0.05f ? 0.3f : dr.Status.Volume,
        //                DeviceKey = deviceKey,
        //                StreamUrl = $"player/stream/{pli.MusicFileId}"

        //            };
        //            dr.CurrentPosition.Set(position);
        //            return await ExecuteCommand(dr, pc);
        //        }
        //        else
        //        {
        //            await PlaylistFinished(deviceKey);
        //            dr.CurrentPosition.Reset();
        //        }
        //    }
        //    return 0;
        //}
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
        public DeviceRuntime GetDeviceRuntime(string key)
        {
            if (runtimeDevices.ContainsKey(key))
            {
                return runtimeDevices[key];
            }
            return null;
        }
        //public async Task ClearPlaylist(string deviceKey)
        //{
        //    var dr = GetDeviceRuntime(deviceKey);
        //    if (dr != null)
        //    {
        //        //dr.Playlist.Items.Clear();
        //        //dr.ExtendedPlaylist.ClearItems();
        //        //dr.CurrentPosition = PlaylistPosition.ZeroPosition;
        //        //var dto = new PlaylistDTO
        //        //{
        //        //    DeviceKey = deviceKey,
        //        //    PlaylistType = dr.Playlist.Type,
        //        //    PlaylistName = dr.Playlist.Name,
        //        //    //DisplayName = dr.DisplayName,
        //        //    Items = dr.Playlist.Items.Select(x => x.ToDTO())
        //        //};
        //        dr.ClearPlaylist();
        //        await SendPlaylist(dr.ToPlaylistDTO());
        //    }
        //}
        public async Task SendDevicePlaylistAsync(string deviceKey)
        {
            var dr = GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                //var dto = new PlaylistDTO
                //{
                //    DeviceKey = deviceKey,
                //    PlaylistType = dr.Playlist.Type,
                //    PlaylistName = dr.Playlist.Name,
                //    //DisplayName = dr.DisplayName,
                //    Items = dr.Playlist.Items.Select(x => x.ToDTO())
                //};
                await SendPlaylist(dr.ToPlaylistDTO());
            }
        }
        public async Task BrowserDisconnectedAsync(string browserKey)
        {
            //using (var scope = new ScopedDbContext<MusicDb>(serviceProvider))
            //{
            //    var device = scope.Db.Devices.SingleOrDefault(x => x.KeyName == browserKey);
            //    if (device != null)
            //    {
            //        RemoveDeviceFromRuntime(device);
            //        await SendDeviceDisabled(device);
            //    }
            //    else
            //    {
            //        log.Warning($"audio for device {browserKey} not running");
            //    }
            //}
            var device = this.libraryService.GetDevice(browserKey);
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
        public async Task AddPlaylistItemAsync<T>(string deviceKey, T entity) where T : EntityBase
        {
            var dr = GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                var playlist = await this.libraryService.GetEntityAsync<Playlist>(dr.ExtendedPlaylist.PlaylistId);
                var pli = await this.libraryService.AddPlaylistItemAsync(playlist, entity);
                dr.ExtendedPlaylist.AddItem(pli, libraryService);
                await SendPlaylist(dr.ToPlaylistDTO());
            }
            else
            {
                log.Error($"device {deviceKey} not found in runtime");
            }
        }
        //public async Task AddPlaylistItem(string deviceKey, PlaylistItem pli)
        //{
        //    var dr = GetDeviceRuntime(deviceKey);
        //    if (dr != null)
        //    {
        //        dr.ExtendedPlaylist.AddItem(pli, libraryService);
        //        await SendPlaylist(dr.ToPlaylistDTO());
        //    }
        //}
        //public async Task AddPlaylistItems(string deviceKey, IEnumerable<PlaylistItem> items)
        //{
        //    var dr = GetDeviceRuntime(deviceKey);
        //    if (dr != null)
        //    {
        //        foreach (var pli in items)
        //        {
        //            dr.ExtendedPlaylist.AddItem(pli, libraryService);
        //        }
        //        await SendPlaylist(dr.ToPlaylistDTO());
        //    }
        //}
        public async Task ResetDevicePlaylistItems(Playlist playlist)
        {
            var deviceRuntimes = FindDevicesUsingPlaylist(playlist.Id);
            foreach (var dr in deviceRuntimes)
            {
                await SetPlaylistAsync(dr, playlist);
            }
        }
        public async Task DeletePlaylistAsync(long playlistId)
        {
            var deviceRuntimes = FindDevicesUsingPlaylist(playlistId);
            foreach (var dr in deviceRuntimes)
            {
                var playlist = await this.libraryService.CreateDevicePlaylistAsync(dr.Key);
                await SetPlaylistAsync(dr, playlist);
            }
            await this.libraryService.DeletePlaylist(playlistId);
        }
        public async Task CopyPlaylistAsync(string fromDeviceKey, string toDeviceKey)
        {
            var from = GetDeviceRuntime(fromDeviceKey);
            var to = GetDeviceRuntime(toDeviceKey);
            if (from == null || to == null)
            {
                if (from == null)
                {
                    log.Error($"from device {fromDeviceKey} not found in runtime");
                }
                if (to == null)
                {
                    log.Error($"to device {toDeviceKey} not found in runtime");
                }
                return;
            }
            var playlist = await this.libraryService.GetEntityAsync<Playlist>(from.ExtendedPlaylist.PlaylistId);
            if (playlist.Type == PlaylistType.DeviceList)
            {
                var copiedItems = playlist.Items.Select(pl => pl.Clone());
                playlist = await this.libraryService.CreateDevicePlaylistAsync(toDeviceKey, copiedItems);
            }
            await SetPlaylistAsync(to, playlist);
        }
        public async Task CreateNamedPlaylistFromDeviceAsync(string deviceKey, string name)
        {
            var dr = GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                var playlist = await this.libraryService.GetEntityAsync<Playlist>(dr.ExtendedPlaylist.PlaylistId);
                var items = playlist.Items.Select(x => x.Clone());
                playlist = await this.libraryService.CreateUserPlaylist(name, items);
                await SetPlaylistAsync(dr, playlist);
            }
            else
            {
                log.Error($"device {deviceKey} not in run time");
            }
            return;
        }
        public IEnumerable<DeviceRuntime> FindDevicesUsingPlaylist(long playlistId)
        {
            //return this.runtimeDevices.Values.Where(x => x.Playlist.Id == playlistId);
            return this.runtimeDevices.Values.Where(x => x.ExtendedPlaylist.PlaylistId == playlistId);
        }
        public async Task SetPlaylistAsync(string deviceKey, long playlistId)
        {
            var pl = await this.libraryService.GetEntityAsync<Playlist>(playlistId);
            await SetPlaylistAsync(deviceKey, pl);
        }
        public async Task SetPlaylistAsync(DeviceRuntime dr, Playlist playlist)
        {
            await this.libraryService.ChangeDevicePlaylist(dr.Key, playlist);
            dr.SetPlaylist(ExtendedPlaylist.Create(playlist, libraryService));
            await SendPlaylist(dr.ToPlaylistDTO());
        }
        public async Task SetPlaylistAsync(string deviceKey, string playlistName)
        {
            var playlist = await this.libraryService.FindPlaylist(playlistName);
            if (playlist != null)
            {
                var dr = GetDeviceRuntime(deviceKey);
                if (dr != null)
                {
                    //dr.SetPlaylist(ExtendedPlaylist.Create(playlist, libraryService));
                    await SetPlaylistAsync(dr, playlist);
                }
                else
                {
                    log.Error($"device {deviceKey} not found in run time");
                }
            }
            else
            {
                log.Error($"playlist {playlistName} not found");
            }
        }
        public async Task SetPlaylistAsync(string deviceKey, Playlist playlist)
        {
            var dr = GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                //dr.SetPlaylist(ExtendedPlaylist.Create(playlist, libraryService));
                await SetPlaylistAsync(dr, playlist);
            }
            else
            {
                log.Error($"device {deviceKey} not found in run time");

            }
        }
        //public async Task SetRuntimePlaylist(Device device, DeviceRuntime dr = null)
        //{
        //    await Task.Delay(0);
        //    if (dr == null)
        //    {
        //        dr = GetDeviceRuntime(device.KeyName);
        //    }
        //    if (dr != null)
        //    {
        //        //dr.Playlist = await ConvertToRuntime(dr, device.Playlist);
        //        dr.SetPlaylist(ExtendedPlaylist.Create(device.Playlist, libraryService));
        //    }
        //    else
        //    {
        //        log.Error($"{device} not found in run time");
        //    }
        //}
        public void DebugDevicePlaylist(string deviceKey)
        {
            var dr = GetDeviceRuntime(deviceKey);
            //DebugDevicePlaylist(dr);
        }
        public void DebugDevicePlaylist(DeviceRuntime dr)
        {
            var epl = dr.ExtendedPlaylist;
            log.Information($"{epl}");
            foreach (var item in epl.Items ?? Enumerable.Empty<ExtendedPlaylistItem>())
            {
                log.Information($"  {item}");
                if (item is MultiTrackPlaylistItem)
                {
                    var mti = item as MultiTrackPlaylistItem;
                    foreach (var subItem in mti.SubItems ?? Enumerable.Empty<ExtendedPlaylistItem>())
                    {
                        log.Information($"    {subItem}");
                    }
                }
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
        //private async Task<int> PlaylistFinished(string deviceKey)
        //{
        //    var pc = new PlayerCommand
        //    {
        //        Command = PlayerCommands.ListFinished,
        //        DeviceKey = deviceKey,
        //    };
        //    return await ExecuteCommand(deviceKey, pc);
        //}
        //private PlaylistItemRuntime GetPlaylistItemAtSequence(DeviceRuntime dr, PlaylistPosition position)
        //private ExtendedPlaylistItem GetPlaylistItemAtSequence(DeviceRuntime dr, PlaylistPosition position)
        //{
        //    //var pli = dr.Playlist.Items.SingleOrDefault(x => x.Sequence == position.Major);
        //    var pli = dr.ExtendedPlaylist.Items.SingleOrDefault(x => x.Position.Major == position.Major);
        //    //if (pli != null && pli.Type == PlaylistRuntimeItemType.MultipleItems)
        //    if (pli != null && pli is MultiTrackPlaylistItem)
        //    {
        //        var mtpli = pli as MultiTrackPlaylistItem;
        //        if (position.Minor == 0)
        //        {
        //            log.Error($"Cannot request a position with Minor == 0 when the playlist item is of type PlaylistRuntimeItemType.MultipleItems");
        //            return null;
        //        }
        //        else
        //        {
        //            //pli = pli.SubItems.SingleOrDefault(x => x.Sequence == position.Minor);
        //            pli = mtpli.SubItems.SingleOrDefault(x => x.Position.Minor == position.Minor);
        //        }
        //    }

        //    return pli;
        //}
        //private (ExtendedPlaylistItem item, PlaylistPosition position) GetNextMinorItem(DeviceRuntime dr, bool reverse)
        //{
        //    var sequence = dr.CurrentPosition.Minor + (reverse ? -1 : 1);         
        //    var majorPli = dr.ExtendedPlaylist.Items.Single(x => x.Position.Major == dr.CurrentPosition.Major);
        //    ExtendedPlaylistItem nextMinorPli = null;
        //    if (majorPli is MultiTrackPlaylistItem)
        //    {
        //        nextMinorPli = (majorPli as MultiTrackPlaylistItem).SubItems.SingleOrDefault(x => x.Position.Minor == sequence);
        //    }
        //    //var nextMinorPli = majorPli.SubItems.SingleOrDefault(x => x.Position.Minor == sequence);
        //    return (nextMinorPli, new PlaylistPosition(dr.CurrentPosition.Major, sequence));
        //}
        //private (PlaylistItemRuntime item, PlaylistPosition position) GetNextMajorItem(DeviceRuntime dr, bool reverse)
        //private (ExtendedPlaylistItem item, PlaylistPosition position) GetNextMajorItem(DeviceRuntime dr, bool reverse)
        //{
        //    var sequence = dr.CurrentPosition.Major + (reverse ? -1 : 1);
        //    var majorPli = dr.ExtendedPlaylist.Items.SingleOrDefault(x => x.Position.Major == sequence);
        //    if (majorPli != null)
        //    {
        //        //if (majorPli.Type == PlaylistRuntimeItemType.MultipleItems)
        //        if (majorPli is MultiTrackPlaylistItem)
        //        {
        //            var nextMinorPli = (majorPli as MultiTrackPlaylistItem).SubItems.Single(x => x.Position.Minor == 1);
        //            return (nextMinorPli, new PlaylistPosition(sequence, 1));
        //        }
        //        else
        //        {
        //            return (majorPli, new PlaylistPosition(sequence, 0));
        //        }
        //    }
        //    else
        //    {
        //        return (null, new PlaylistPosition(sequence, 0));
        //    }
        //}
        //private (PlaylistItemRuntime item, PlaylistPosition position) GetNextPlaylistItem(DeviceRuntime dr, bool reverse)
        //private (ExtendedPlaylistItem item, PlaylistPosition position) GetNextPlaylistItem(DeviceRuntime dr, bool reverse)
        //{
        //    try
        //    {
        //        if (!dr.CurrentPosition.IsZero())
        //        {
        //            var majorPli = dr.ExtendedPlaylist.Items.Single(x => x.Position.Major == dr.CurrentPosition.Major);
        //            //if (majorPli.Type == PlaylistRuntimeItemType.MultipleItems)
        //            if (majorPli is MultiTrackPlaylistItem)
        //            {
        //                (var item, var position) = GetNextMinorItem(dr, reverse);
        //                if (item != null)
        //                {
        //                    // we have a minor item
        //                    return (item, position);
        //                }
        //            }
        //        }
        //        return GetNextMajorItem(dr, reverse);
        //    }
        //    catch (Exception xe)
        //    {
        //        log.Error(xe);
        //    }
        //    return (null, null);
        //}
        public async Task OnDeviceStatusAsync(DeviceStatus deviceStatus)
        {
            try
            {
                var dr = GetDeviceRuntime(deviceStatus.Key);
                if (dr != null)
                {
                    try
                    {
                        dr.Status = deviceStatus;
                        //var dto = deviceStatus.ToDTO(dr);
                        //await this.messageHub.Clients.All.SendDeviceStatus(dto);
                        //log.Debug($"{dr.DisplayName}, {deviceStatus.ToString()}");
                        await SendDeviceStatus(dr);
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
            //lock (runtimeDevices)
            //{
            //    this.runtimeDevices.Remove(device.KeyName);
            //}
            if (this.runtimeDevices.TryRemove(device.KeyName, out _))
            {
                this.keepAlive.SetPlayerUrls(this.GetPlayerUrls());
                log.Information($"Device {device.DisplayName} removed from run time");
            }
        }
        private async Task AddDeviceToRuntime(Device device)
        {
            if (this.keepAlive != null)
            {
                DeviceRuntime dr = GetDeviceRuntime(device.KeyName);
                if (dr == null)
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
                    //dr.Playlist = await ConvertToRuntime(dr, device.Playlist);
                    //await SetRuntimePlaylist(device, dr);
                    await this.SetPlaylistAsync(dr, device.Playlist);
                    //DebugDevicePlaylist(dr);
                    if (this.runtimeDevices.TryAdd(dr.Key, dr))
                    {
                        log.Information($"{device} added to run time");
                        await PlayerResetAsync(dr.Key);
                    }
                    else
                    {
                        log.Error($"failed to add device {device} to run time");
                    }
                }
                else
                {
                    log.Warning($"{device} already present in run time");
                }

                this.keepAlive.SetPlayerUrls(this.GetPlayerUrls());
            }
            else
            {
                log.Warning($"not ready to add a device - keepAlive not yet started");
            }
        }
        //private void LogDevices()
        //{
        //    lock (runtimeDevices)
        //    {
        //        log.Information($"runtime is {this.runtimeDevices.Count} device(s):");
        //        foreach (var item in this.runtimeDevices)
        //        {
        //            var dr = item.Value;
        //            log.Information($"    {dr.Key}, {dr.DisplayName}, type {dr.Type.ToString()}");
        //        }
        //    }
        //}
        private string[] GetPlayerUrls()
        {
            //using (var scope = new ScopedDbContext<MusicDb>(serviceProvider))
            //{
            //    var urls = scope.Db.Devices.Where(x => !x.IsDisabled && x.Type != AudioDeviceType.Browser && !string.IsNullOrWhiteSpace(x.PlayerUrl))
            //        .Select(x => x.PlayerUrl).Distinct();
            //    return urls.ToArray();
            //}
            var urls = this.libraryService.GetAllDevices()
                .Where(x => !x.IsDisabled && x.Type != AudioDeviceType.Browser && !string.IsNullOrWhiteSpace(x.PlayerUrl))
                .Select(x => x.PlayerUrl)
                .Distinct();
            return urls.ToArray();
        }
        private async Task PlayerDead(string url)
        {
            var affectedDevices = this.runtimeDevices.Select(x => x.Value).Where(x => string.Compare(x.PlayerUrl, url, true) == 0);
            //using (var scope = new ScopedDbContext<MusicDb>(serviceProvider))
            //{
            //    foreach (var dr in affectedDevices.ToArray())
            //    {
            //        var device = await scope.Db.Devices.SingleAsync(x => x.KeyName == dr.Key);
            //        log.Information($"Device {device.DisplayName} to be removed from run time as polling has failed ... url was {url}");
            //        RemoveDeviceFromRuntime(device);
            //        await SendDeviceDisabled(device);

            //    }
            //}
            foreach (var dr in affectedDevices.ToArray())
            {
                var device = this.libraryService.GetDevice(dr.Key);
                log.Information($"Device {device.DisplayName} to be removed from run time as polling has failed ... url was {url}");
                RemoveDeviceFromRuntime(device);
                await SendDeviceDisabled(device);
            }
        }
        //private async Task<PlaylistItemRuntime> ConvertToRuntime(DeviceRuntime dr, PlaylistItem playlistItem)
        //{
        //    async Task<PlaylistItemRuntime> FromPerformance()
        //    {
        //        var performance = await this.libraryService.GetEntityAsync<Performance>(playlistItem.ItemId);
        //        if (performance != null)
        //        {
        //            var movements = performance.Movements.OrderBy(m => m.MovementNumber);
        //            var subItems = new List<PlaylistItemRuntime>();
        //            var index = 0;
        //            foreach (var movement in movements)
        //            {
        //                var subItem = FromTrack(dr, movement, new PlaylistPosition(playlistItem.Sequence, ++index));
        //                subItems.Add(subItem);
        //            }
        //            var plir = new PlaylistItemRuntime
        //            {
        //                Type = PlaylistRuntimeItemType.MultipleItems,
        //                Position = new PlaylistPosition(playlistItem.Sequence, 0),
        //                Titles = new string[] { playlistItem.Title },
                        
        //                CoverArtUrl = $"lib/get/work/coverart/{movements.First().Work.Id}",
        //                SubItems = subItems
        //            };
        //            plir.TotalTime = plir.SubItems.Sum(x => x.TotalTime);
        //            plir.FormattedTotalTime = plir.TotalTime.FormatDuration();
        //            return plir; 
        //        }
        //        else
        //        {
        //            log.Warning($"{playlistItem} is not valid");
        //        }
        //        return null;
        //    }
        //    async Task<PlaylistItemRuntime> FromWork()
        //    {
        //        var work = await this.libraryService.GetEntityAsync<Work>(playlistItem.ItemId);
        //        if (work != null)
        //        {
        //            var tracks = work.Tracks.OrderBy(t => t.Number);
        //            var subItems = new List<PlaylistItemRuntime>();
        //            var index = 0;
        //            foreach (var track in tracks)
        //            {
        //                var subItem = FromTrack(dr, track, new PlaylistPosition(playlistItem.Sequence, ++index));
        //                subItems.Add(subItem);
        //            }
        //            var plir = new PlaylistItemRuntime
        //            {
        //                Type = PlaylistRuntimeItemType.MultipleItems,
        //                Position = new PlaylistPosition(playlistItem.Sequence, 0),
        //                Titles = new string[] { playlistItem.Title },
        //                CoverArtUrl = $"lib/get/work/coverart/{work.Id}",
        //                SubItems = subItems
        //            };
        //            plir.TotalTime = plir.SubItems.Sum(x => x.TotalTime);
        //            plir.FormattedTotalTime = plir.TotalTime.FormatDuration();
        //            return plir; 
        //        }
        //        else
        //        {
        //            log.Warning($"{playlistItem} is not valid");
        //        }
        //        return null;
        //    }
        //    async Task<PlaylistItemRuntime> FromMusicFile()
        //    {
        //        var mf = await this.libraryService.GetEntityAsync<MusicFile>(playlistItem.MusicFileId);
        //        if (mf != null)
        //        {
        //            return new PlaylistItemRuntime
        //            {
        //                Type = PlaylistRuntimeItemType.SingleItem,
        //                Position = new PlaylistPosition(playlistItem.Sequence, 0),
        //                Titles = new string[] {
        //                        mf.Track.Performance?.GetParentArtistsName() ?? mf.Track.Work.Artists.Select(a => a.Name).ToCSV(),
        //                        mf.Track.Performance?.GetParentEntityDisplayName() ?? mf.Track.Work.Name,
        //                        mf.Track.Title
        //                    },
        //                NotPlayableOnCurrentDevice = !(dr.MaxSampleRate == 0 || mf.SampleRate == 0 || mf.SampleRate <= dr.MaxSampleRate),
        //                MusicFileId = mf.Id,
        //                AudioProperties = mf.GetAudioProperties(),
        //                SampleRate = mf.SampleRate ?? 0,
        //                TotalTime = mf.Duration ?? 0.0,
        //                FormattedTotalTime = mf.Duration?.FormatDuration() ?? "00:00",
        //                CoverArtUrl = $"lib/get/work/coverart/{mf.Track.Work.Id}"
        //            };
        //        }
        //        else
        //        {
        //            log.Warning($"{playlistItem} is not valid");
        //        }
        //        return null;
        //    }

        //    return playlistItem.Type switch
        //    {
        //        PlaylistItemType.MusicFile => await FromMusicFile(),
        //        PlaylistItemType.Track => await FromTrack(dr, playlistItem),
        //        PlaylistItemType.Work => await FromWork(),
        //        PlaylistItemType.Performance => await FromPerformance(),
        //        _ => throw new Exception($"Unexpected PlaylistItemType {playlistItem.Type}"),
        //    };
        //}
        //private async Task<PlaylistRuntime> ConvertToRuntime(DeviceRuntime dr, Playlist playlist)
        //{
        //    async Task<List<PlaylistItemRuntime>> GetPlaylistItemRuntimes()
        //    {
        //        var runtimeItems = new List<PlaylistItemRuntime>();
        //        //var index = 0;
        //        foreach (var pli in playlist.Items)
        //        {
        //            var runtimeItem = await ConvertToRuntime(dr, pli/*, index++*/);
        //            if (runtimeItem != null)
        //            {
        //                runtimeItems.Add(runtimeItem);
        //            }
        //        }
        //        return runtimeItems;
        //    }

        //    // **NB** async within a select with a .WhenAll does not work with efcore dbcontext
        //    // because it causes multiple tasks on different threads which are not supported for dbcontext
        //    //var items = await Task.WhenAll(list.Items.Select(async x => await ConvertToRuntime(dr, x)));

        //    List<PlaylistItemRuntime> runtimeItems = await GetPlaylistItemRuntimes();
        //    return new PlaylistRuntime
        //    {
        //        Id = playlist.Id,
        //        Name = playlist.Name,
        //        Type = playlist.Type,
        //        Items = runtimeItems
        //            .OrderBy(x => x.Position.Major).ToList()
        //    };
        //}
        //private async Task<PlaylistItemRuntime> FromTrack(DeviceRuntime dr, PlaylistItem playlistItem)
        //{
        //    var track = await this.libraryService.GetEntityAsync<Track>(playlistItem.ItemId);
        //    return FromTrack(dr, track, new PlaylistPosition(playlistItem.Sequence, 0));
        //}
        //private PlaylistItemRuntime FromTrack(DeviceRuntime dr, Track track, PlaylistPosition position)
        //{
        //    var mf = track.GetBestMusicFile(dr);
        //    var title = track.Title;
        //    if(position.Minor > 0 && title.Contains(':'))
        //    {
        //        title = title.Split(':').Skip(1).First();
        //    }
        //    return new PlaylistItemRuntime
        //    {
        //        Type = PlaylistRuntimeItemType.SingleItem,
        //        Position = position,
        //        Titles = new string[] {
        //                        track.Performance?.GetParentArtistsName() ?? track.Work.Artists.Select(a => a.Name).ToCSV(),
        //                        track.Performance?.GetParentEntityDisplayName() ?? track.Work.Name,
        //                        title 
        //                    },
        //        NotPlayableOnCurrentDevice = mf == null,
        //        MusicFileId = mf?.Id ?? 0,
        //        AudioProperties = mf?.GetAudioProperties(),
        //        SampleRate = mf?.SampleRate ?? 0,
        //        TotalTime = mf?.Duration ?? 0.0,
        //        FormattedTotalTime = mf?.Duration?.FormatDuration() ?? "00:00",
        //        CoverArtUrl = $"lib/get/work/coverart/{track.Work.Id}"
        //    };
        //}
    }
}
