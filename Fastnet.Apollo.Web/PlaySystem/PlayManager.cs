﻿using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Messages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    public partial class PlayManager : HostedService // RealtimeTask
    {
        #region Private Fields

        private readonly ILoggerFactory lf;
        private readonly LibraryService libraryService;
        private readonly DeviceService deviceService;
        //private readonly IHubContext<MessageHub, IHubMessage> messageHub;
        //private readonly Messenger messenger;
        //private readonly MessengerOptions messengerOptions;
        private readonly MusicServerOptions musicServerOptions;
        //private readonly ConcurrentDictionary<string, DeviceRuntime> runtimeDevices;
        private readonly List<Task> taskList;
        //private KeepAgentsAlive keepAlive;
        //private ServerInformationMulticast sim;

        #endregion Private Fields

        #region Public Constructors

        public PlayManager(
            LibraryService ls,
            DeviceService deviceService,
            //IHubContext<MessageHub, IHubMessage> messageHub,
            //IWebHostEnvironment environment, IConfiguration cfg,
            IOptions<MusicServerOptions> serverOptions, /*Messenger messenger,*/ /*IOptions<MessengerOptions> messengerOptions,*/
            ILogger<PlayManager> log, ILoggerFactory loggerFactory) : base(log)
        {
            //this.messengerOptions = messengerOptions.Value;
            //this.messageHub = messageHub;
            this.libraryService = ls;
            this.deviceService = deviceService;
            this.musicServerOptions = serverOptions.Value;
            //this.messenger = messenger;
            //this.messenger.EnableMulticastSend();
            //var connectionString = environment.LocaliseConnectionString(cfg.GetConnectionString("MusicDb"));
            // playmanager is a singleton, so it cannot use an injected scoped LibraryService
            //this.libraryService = new LibraryService(serverOptions, messageHub, loggerFactory.CreateLogger<LibraryService>(), new MusicDb(connectionString));
            this.lf = loggerFactory;
            this.taskList = new List<Task>();
            //this.runtimeDevices = new ConcurrentDictionary<string, DeviceRuntime>();// new Dictionary<string, DeviceRuntime>();
        }

        #endregion Public Constructors

        #region Public Methods

        public async Task AddPlaylistItemAsync<T>(string deviceKey, T entity) where T : EntityBase
        {
            //var epl = this.deviceService.GetExtendedPlaylist(deviceKey);
            //var playlist = await this.libraryService.GetEntityAsync<Playlist>(epl.PlaylistId);
            var playlist = await GetPlaylist(deviceKey);
            var pli = await this.libraryService.AddPlaylistItemAsync(playlist, entity);
            if (pli != null)
            {
                var epli = this.libraryService.CreateExtendedPlaylistItem(pli);
                await this.deviceService.AddExtendedPlaylistItem(deviceKey, epli);
            }
            else
            {
                log.Error("playlistitem not created");
            }
            //var dr = GetDeviceRuntime(deviceKey);
            //if (dr != null)
            //{
            //    //var playlist = await this.libraryService.GetEntityAsync<Playlist>(dr.ExtendedPlaylist.PlaylistId);
            //    var epl = this.deviceService.GetPlaylist(deviceKey);
            //    var playlist = await this.libraryService.GetEntityAsync<Playlist>(epl.PlaylistId);
            //    var pli = await this.libraryService.AddPlaylistItemAsync(playlist, entity);
            //    if (pli != null)
            //    {
            //        var epli = this.libraryService.CreateExtendedPlaylistItem(pli);
            //        //this.libraryService.AddPlaylistItem(dr.ExtendedPlaylist, pli);
            //        //dr.ExtendedPlaylist.AddItem(pli, libraryService);
            //        //await SendPlaylist(dr.ToPlaylistDTO());
            //        //await this.deviceService.SendPlaylist(dr.ToPlaylistDTO());
            //        await this.deviceService.AddPlaylistItem(deviceKey, epli);
            //    }
            //    else
            //    {
            //        log.Error("playlistitem not created");
            //    }
            //}
            //else
            //{
            //    log.Error($"device {deviceKey} not found in runtime");
            //}
        }
        //public async Task BrowserDisconnectedAsync(string browserKey)
        //{
        //    var device = this.libraryService.GetDevice(browserKey);
        //    if (device != null)
        //    {
        //        RemoveDeviceFromRuntime(device);
        //        //await SendDeviceDisabled(device);
        //        await this.deviceService.SendDeviceDisabled(browserKey);
        //    }
        //    else
        //    {
        //        log.Warning($"audio for device {browserKey} not running");
        //    }
        //}
        public async Task CopyPlaylistAsync(string fromDeviceKey, string toDeviceKey)
        {
            //var from = GetDeviceRuntime(fromDeviceKey);
            //var to = GetDeviceRuntime(toDeviceKey);
            //if (from == null || to == null)
            //{
            //    if (from == null)
            //    {
            //        log.Error($"from device {fromDeviceKey} not found in runtime");
            //    }
            //    if (to == null)
            //    {
            //        log.Error($"to device {toDeviceKey} not found in runtime");
            //    }
            //    return;
            //}
            if (this.deviceService.IsPresentInRuntime(fromDeviceKey) && this.deviceService.IsPresentInRuntime(toDeviceKey))
            {
                //var playlist = await this.libraryService.GetEntityAsync<Playlist>(from.ExtendedPlaylist.PlaylistId);
                //var epl = this.deviceService.GetExtendedPlaylist(fromDeviceKey);
                //var playlist = await this.libraryService.GetEntityAsync<Playlist>(epl.PlaylistId);
                var playlist = await GetPlaylist(fromDeviceKey);
                if (playlist.Type == PlaylistType.DeviceList)
                {
                    var copiedItems = playlist.Items.Select(pl => pl.Clone());
                    playlist = await this.libraryService.CreateDevicePlaylistAsync(toDeviceKey, copiedItems);
                }
                await SetPlaylistAsync(toDeviceKey, playlist);
            }
            else
            {
                log.Error($"Either to key {toDeviceKey} and/or from key {fromDeviceKey} are not valid");
            }
        }
        public async Task CreateNamedPlaylistFromDeviceAsync(string deviceKey, string name)
        {
            var playlist = await GetPlaylist(deviceKey);
            var items = playlist.Items.Select(x => x.Clone());
            playlist = await this.libraryService.CreateUserPlaylist(name, items);
            await SetPlaylistAsync(deviceKey, playlist);
            //var dr = GetDeviceRuntime(deviceKey);
            //if (dr != null)
            //{
            //    var playlist = await this.libraryService.GetEntityAsync<Playlist>(dr.ExtendedPlaylist.PlaylistId);
            //    var items = playlist.Items.Select(x => x.Clone());
            //    playlist = await this.libraryService.CreateUserPlaylist(name, items);
            //    await SetPlaylistAsync(dr, playlist);
            //}
            //else
            //{
            //    log.Error($"device {deviceKey} not in run time");
            //}
            return;
        }
        public async Task DeletePlaylistAsync(long playlistId)
        {
            var keys = this.deviceService.FindDevicesUsingPlaylist(playlistId);
            foreach (var key in keys)
            {
                var playlist = await this.libraryService.CreateDevicePlaylistAsync(key);
                await SetPlaylistAsync(key, playlist);
            }
            //var deviceRuntimes = FindDevicesUsingPlaylist(playlistId);
            //foreach (var dr in deviceRuntimes)
            //{
            //    var playlist = await this.libraryService.CreateDevicePlaylistAsync(dr.Key);
            //    await SetPlaylistAsync(dr, playlist);
            //}
            await this.libraryService.DeletePlaylist(playlistId);
        }
        //public IEnumerable<DeviceRuntime> FindDevicesUsingPlaylist(long playlistId)
        //{
        //    //return this.runtimeDevices.Values.Where(x => x.Playlist.Id == playlistId);
        //    return this.runtimeDevices.Values.Where(x => x.ExtendedPlaylist.PlaylistId == playlistId);
        //}
        //public DeviceRuntime GetDeviceRuntime(string key)
        //{
        //    if (runtimeDevices.ContainsKey(key))
        //    {
        //        return runtimeDevices[key];
        //    }
        //    return null;
        //}
        public DeviceStatusDTO GetDeviceStatus(string deviceKey)
        {
            return this.deviceService.GetDeviceStatus(deviceKey);
        }
        public IEnumerable<DeviceRuntime> GetDeviceRuntimes()
        {
            //return this.runtimeDevices.Values.ToArray();
            return this.deviceService.GetDeviceRuntimes();
        }
        public async Task SendDeviceStatus(DeviceStatus ds)
        {
            await this.deviceService.SendDeviceStatus(ds);
        }
        //public async Task OnDeviceStatusAsync(DeviceStatus deviceStatus)
        //{
        //    try
        //    {
        //        var dr = GetDeviceRuntime(deviceStatus.Key);
        //        if (dr != null)
        //        {
        //            try
        //            {
        //                dr.Status = deviceStatus;
        //                //await SendDeviceStatus(dr);
        //                await this.deviceService.SendDeviceStatus(dr.Status.ToDTO(dr));
        //            }
        //            catch (Exception xe)
        //            {
        //                log.Error(xe);
        //            }
        //        }
        //        else
        //        {
        //            log.Debug($"Update received from device {deviceStatus.Key}, state {deviceStatus.State} - not found in device run time");
        //        }
        //    }
        //    catch (Exception xe)
        //    {
        //        log.Error(xe);
        //    }
        //}
        public async Task<int> PlayNextAsync(string deviceKey, bool reverse = false)
        {
            return await this.deviceService.PlayNextAsync(deviceKey, reverse);
            //var dr = GetDeviceRuntime(deviceKey);
            //if (dr != null)
            //{
            //    var nextItem = reverse ? dr.GetPreviousItem() : dr.GetNextItem();
            //    if (nextItem != null)
            //    {
            //        PlayerCommand pc = null;
            //        pc = new PlayerCommand
            //        {
            //            Command = PlayerCommands.Play,
            //            Volume = dr.Status.Volume < 0.05f ? 0.3f : dr.Status.Volume,
            //            DeviceKey = deviceKey,
            //            StreamUrl = $"player/stream/{nextItem.MusicFileId}"
            //        };
            //        //dr.CurrentPosition.Set(position);
            //        return await ExecuteCommandAsync(dr, pc);
            //    }
            //}
            //return 0;
        }
        public async Task<int> PlaySequenceNumberAsync(string deviceKey, PlaylistPosition position)
        {
            return await this.deviceService.PlaySequenceNumberAsync(deviceKey, position);
            //var dr = GetDeviceRuntime(deviceKey);
            //if (dr != null)
            //{
            //    var stpli = dr.GetItemAtPosition(position);
            //    if (stpli != null)
            //    {
            //        var pc = new PlayerCommand
            //        {
            //            Command = PlayerCommands.Play,
            //            Volume = dr.Status.Volume < 0.05f ? 0.3f : dr.Status.Volume,
            //            DeviceKey = deviceKey,
            //            StreamUrl = $"player/stream/{stpli.MusicFileId}"
            //        };
            //        //dr.CurrentPosition.Set(position);
            //        dr.CurrentPosition = position;
            //        return await ExecuteCommandAsync(dr, pc);
            //    }
            //    else
            //    {
            //        log.Warning($"Device {dr.DisplayName} no playlist item at sequence {position}");
            //        dr.CommandSequenceNumber = (dr.CommandSequenceNumber + 1) % 1024;
            //    }
            //}
            //return dr.CommandSequenceNumber;// 0;
        }
        public async Task ResetDevicePlaylistItems(Playlist playlist)
        {
            var keys = this.deviceService.FindDevicesUsingPlaylist(playlist.Id);
            foreach (var key in keys)
            {
                await SetPlaylistAsync(key, playlist);
            }
            //var deviceRuntimes = FindDevicesUsingPlaylist(playlist.Id);
            //foreach (var dr in deviceRuntimes)
            //{
            //    await SetPlaylistAsync(dr, playlist);
            //}
        }
        public async Task SendDevicePlaylistAsync(string deviceKey)
        {
            await this.deviceService.SendPlaylist(deviceKey);
            //var dr = GetDeviceRuntime(deviceKey);
            //if (dr != null)
            //{
            //    //await SendPlaylist(dr.ToPlaylistDTO());
            //    await this.deviceService.SendPlaylist(dr.ToPlaylistDTO());
            //}
        }
        public async Task SendDeviceStatus(string deviceKey)
        {
            await this.deviceService.SendDeviceStatus(deviceKey);
            //var dr = GetDeviceRuntime(deviceKey);
            //if (dr != null)
            //{
            //    await this.deviceService.SendDeviceStatus(dr.Status.ToDTO(dr));
            //}
            //else
            //{
            //    log.Error($"device with key {deviceKey} not found");
            //}
        }
        public async Task SetPlaylistAsync(string deviceKey, long playlistId)
        {
            var pl = await this.libraryService.GetEntityAsync<Playlist>(playlistId);
            await SetPlaylistAsync(deviceKey, pl);
        }
        //public async Task SetPlaylistAsync(DeviceRuntime dr, Playlist playlist)
        //{
        //    await this.libraryService.ChangeDevicePlaylist(dr.Key, playlist);
        //    var epl = this.libraryService.CreateExtendedPlaylist(playlist);
        //    await this.deviceService.SetExtendedPlaylist(dr.Key, epl);
        //}
        public async Task SetPlaylistAsync(string deviceKey, Playlist playlist)
        {
            await this.libraryService.ChangeDevicePlaylist(deviceKey, playlist);
            var epl = this.libraryService.CreateExtendedPlaylist(playlist);
            await this.deviceService.SetExtendedPlaylist(deviceKey, epl);
        }
        public async Task SetPlaylistAsync(string deviceKey, string playlistName)
        {
            var playlist = await this.libraryService.FindPlaylist(playlistName);
            await SetPlaylistAsync(deviceKey, playlist);
            //if (playlist != null)
            //{
            //    var dr = GetDeviceRuntime(deviceKey);
            //    if (dr != null)
            //    {
            //        //dr.SetPlaylist(ExtendedPlaylist.Create(playlist, libraryService));
            //        await SetPlaylistAsync(dr, playlist);
            //    }
            //    else
            //    {
            //        log.Error($"device {deviceKey} not found in run time");
            //    }
            //}
            //else
            //{
            //    log.Error($"playlist {playlistName} not found");
            //}
        }
        //public async Task SetPlaylistAsync(string deviceKey, Playlist playlist)
        //{
        //    var dr = GetDeviceRuntime(deviceKey);
        //    if (dr != null)
        //    {
        //        //dr.SetPlaylist(ExtendedPlaylist.Create(playlist, libraryService));
        //        await SetPlaylistAsync(dr, playlist);
        //    }
        //    else
        //    {
        //        log.Error($"device {deviceKey} not found in run time");
        //    }
        //}
        public async Task<int> SetPositionAsync(string deviceKey, float position)
        {
            return await this.deviceService.SetPositionAsync(deviceKey, position);
            //var pc = new PlayerCommand
            //{
            //    Command = PlayerCommands.SetPosition, //.JumpTo,
            //    DeviceKey = deviceKey,
            //    Position = position
            //};
            //return await ExecuteCommandAsync(deviceKey, pc);
        }
        public async Task<int> SetVolumeAsync(string deviceKey, float volume)
        {
            return await this.deviceService.SetVolumeAsync(deviceKey, volume);
            //var pc = new PlayerCommand
            //{
            //    Command = PlayerCommands.SetVolume,
            //    DeviceKey = deviceKey,
            //    Volume = volume
            //};
            //return await ExecuteCommandAsync(deviceKey, pc);
        }
        public async Task<int> TogglePlayPauseAsync(string deviceKey)
        {
            int sn = 0;
            var ds = this.deviceService.GetDeviceStatus(deviceKey);
            switch (ds.State)
            {
                case PlayerStates.Paused:
                case PlayerStates.Playing:
                    sn = await this.deviceService.TogglePlayPauseAsync(deviceKey); ;// await playManager.TogglePlayPauseAsync(deviceKey);
                    break;
                case PlayerStates.Idle:
                case PlayerStates.SilentIdle:
                    sn = await PlayNextAsync(deviceKey);// await PlayNextItem(deviceKey);
                    break;
                default:
                    log.Warning($"{deviceKey} TogglePlayPause in state {ds.State}");
                    break;
            }
            return sn;
            //var dr = GetDeviceRuntime(deviceKey);
            //if (dr != null)
            //{
            //    int sn = 0;
            //    switch (dr.Status.State)
            //    {
            //        case PlayerStates.Paused:
            //        case PlayerStates.Playing:
            //            sn = await this.deviceService.TogglePlayPauseAsync(deviceKey); ;// await playManager.TogglePlayPauseAsync(deviceKey);
            //            break;
            //        case PlayerStates.Idle:
            //        case PlayerStates.SilentIdle:
            //            sn = await PlayNextAsync(deviceKey);// await PlayNextItem(deviceKey);
            //            break;
            //        default:
            //            log.Warning($"{dr.DisplayName} TogglePlayPause in state {dr.Status.State}");
            //            break;
            //    }
            //    return sn;
            //}
            //return 0;
        }
        public async Task UpdateDeviceRuntimeAsync(Device device)
        {
            await this.deviceService.UpdateDeviceRuntimeAsync(device.ToDTO(), device.IsDisabled);
            //var dr = GetDeviceRuntime(device.KeyName);
            //if (dr != null)
            //{
            //    // device is present in the run time
            //    if (device.IsDisabled)
            //    {
            //        RemoveDeviceFromRuntime(device);
            //        //await SendDeviceDisabled(device);
            //        await this.deviceService.SendDeviceDisabled(device.KeyName);
            //    }
            //    else
            //    {
            //        if (dr.DisplayName != device.DisplayName)
            //        {
            //            var oldName = dr.DisplayName;
            //            dr.DisplayName = device.DisplayName;
            //            //await SendDeviceNameChanged(device);
            //            await this.deviceService.SendDeviceNameChanged(device.ToDTO());
            //            log.Information($"Device {oldName} named changed to {device.DisplayName}");
            //        }
            //        // resync with the clients, just in case ...
            //        //await SendDeviceEnabled(device);
            //        await this.deviceService.SendDeviceEnabled(device.ToDTO());
            //    }
            //}
            //else
            //{
            //    // device is not present in the run time
            //    if (!device.IsDisabled/* && device.IsAvailable*/)
            //    {
            //        //await AddDeviceToRuntime(device);
            //        //await SendDeviceEnabled(device);
            //        await this.deviceService.AddDevice(device.ToDTO());
            //        await this.deviceService.SendDeviceEnabled(device.ToDTO());
            //    }
            //}
            //log.Debug($"devices runtime contains {this.runtimeDevices.Count()} device");
        }

        #endregion Public Methods

        #region Protected Methods

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            //messenger.AddMulticastSubscription<DeviceStatus>(async (m) => { await OnDeviceStatusAsync(m); });
            //this.taskList.Add(Task.Run(() =>
            //{
            //    this.sim = new ServerInformationMulticast(/*environment,*/ messengerOptions, musicServerOptions, messenger, cancellationToken, lf);
            //    sim.Start().ContinueWith(sim.OnException, TaskContinuationOptions.OnlyOnFaulted);
            //    this.keepAlive = new KeepAgentsAlive(musicServerOptions, cancellationToken, lf);
            //    this.keepAlive.Start(this.GetPlayerUrls(), async (url) =>
            //    {
            //        await PlayerDead(url);
            //    }).ContinueWith(keepAlive.OnException, TaskContinuationOptions.OnlyOnFaulted);
            //}, cancellationToken));
            //await Task.WhenAll(taskList);
            await Task.Delay(-1, cancellationToken);
        }

        #endregion Protected Methods

        #region Private Methods
        private async Task<Playlist> GetPlaylist(string deviceKey)
        {
            var epl = this.deviceService.GetExtendedPlaylist(deviceKey);
            return await this.libraryService.GetEntityAsync<Playlist>(epl.PlaylistId);
        }
        public async Task AddDeviceToRuntime(Device device)
        {
            if (!device.IsDisabled && !this.deviceService.IsPresentInRuntime(device.KeyName))
            {
                var epl = this.libraryService.CreateExtendedPlaylist(device.Playlist);
                await this.deviceService.AddDevice(device.ToDTO(), epl);
                await this.deviceService.SetExtendedPlaylist(device.KeyName, epl);
            }
        }
        //private async Task<int> ExecuteCommandAsync(string key, PlayerCommand playerCommand/*, Action<DeviceRuntime> afterExecute = null*/)
        //{
        //    var dr = GetDeviceRuntime(key);
        //    return await ExecuteCommandAsync(dr, playerCommand/*, afterExecute*/);
        //}
        //private async Task<int> ExecuteCommandAsync(DeviceRuntime dr, PlayerCommand playerCommand/*, Action<DeviceRuntime> afterExecute = null*/)
        //{
        //    PlayerClient GetPlayerClient(DeviceRuntime deviceRuntime)
        //    {
        //        return new PlayerClient(deviceRuntime.PlayerUrl, lf.CreateLogger<PlayerClient>());
        //    }
        //    try
        //    {
        //        if (dr != null)
        //        {
        //            dr.CommandSequenceNumber = (dr.CommandSequenceNumber + 1) % 1024;
        //            dr.MostRecentCommand = playerCommand;
        //            playerCommand.CommandSequenceNumber = dr.CommandSequenceNumber;
        //            if (dr.Type != AudioDeviceType.Browser)
        //            {
        //                using (var deviceClient = GetPlayerClient(dr))
        //                {
        //                    await deviceClient.Execute(playerCommand);
        //                }
        //            }
        //            else
        //            {
        //                await this.messageHub.Clients.Group("WebAudio").SendCommand(playerCommand);
        //            }
        //            log.Debug($"{dr.DisplayName}: command {playerCommand.Command}, command sequence {playerCommand.CommandSequenceNumber}");
        //            return playerCommand.CommandSequenceNumber;
        //        }
        //    }
        //    catch (Exception xe)
        //    {
        //        log.Error(xe);
        //    }
        //    return 0;
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
        //private async Task PlayerDead(string url)
        //{
        //    var affectedDevices = this.runtimeDevices.Select(x => x.Value).Where(x => string.Compare(x.PlayerUrl, url, true) == 0);
        //    foreach (var dr in affectedDevices.ToArray())
        //    {
        //        var device = this.libraryService.GetDevice(dr.Key);
        //        log.Information($"Device {device.DisplayName} to be removed from run time as polling has failed ... url was {url}");
        //        RemoveDeviceFromRuntime(device);
        //        //await SendDeviceDisabled(device);
        //        await this.deviceService.SendDeviceDisabled(device.KeyName);
        //    }
        //}
        //private async Task<int> PlayerResetAsync(string deviceKey)
        //{
        //    var pc = new PlayerCommand
        //    {
        //        Command = PlayerCommands.Reset,
        //        DeviceKey = deviceKey,
        //    };
        //    return await ExecuteCommandAsync(deviceKey, pc);
        //}
        //private void RemoveDeviceFromRuntime(Device device)
        //{
        //    if (this.runtimeDevices.TryRemove(device.KeyName, out _))
        //    {
        //        //this.keepAlive.SetPlayerUrls(this.GetPlayerUrls());
        //        log.Information($"Device {device.DisplayName} removed from run time");
        //    }
        //}

        #endregion Private Methods

        #region debugging methods

        //public void DebugDevicePlaylist(string deviceKey)
        //{
        //    var dr = GetDeviceRuntime(deviceKey);
        //    //DebugDevicePlaylist(dr);
        //}
        //public void DebugDevicePlaylist(DeviceRuntime dr)
        //{
        //    var epl = dr.ExtendedPlaylist;
        //    log.Information($"{epl}");
        //    foreach (var item in epl.Items ?? Enumerable.Empty<ExtendedPlaylistItem>())
        //    {
        //        log.Information($"  {item}");
        //        if (item is MultiTrackPlaylistItem)
        //        {
        //            var mti = item as MultiTrackPlaylistItem;
        //            foreach (var subItem in mti.SubItems ?? Enumerable.Empty<ExtendedPlaylistItem>())
        //            {
        //                log.Information($"    {subItem}");
        //            }
        //        }
        //    }
        //}

        #endregion debugging methods
    }

    internal static class PlayManagerExtensions
    {
        #region Public Methods

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

        #endregion Public Methods
    }
}