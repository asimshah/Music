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
    public partial class PlayManager : HostedService // RealtimeTask
    {
        //private readonly IDictionary<string, DeviceRuntime> runtimeDevices;
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
        public PlayManager(IHubContext<MessageHub, IHubMessage> messageHub, /*IServiceProvider serviceProvider,*/
            //SingletonLibraryService libraryService,
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
            messenger.AddMulticastSubscription<DeviceStatus>(async (m) => { await OnDeviceStatus(m); });
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
            log.Debug($"devices runtime contains {this.runtimeDevices.Count()} device");
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
            if (runtimeDevices.ContainsKey(key))
            {
                return runtimeDevices[key];
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
        public async Task AddPlaylistItem(string deviceKey, PlaylistItem pli)
        {
            //using (var scope = new ScopedDbContext<MusicDb>(serviceProvider))
            //{
            //    var dr = GetDeviceRuntime(deviceKey);
            //    if (dr != null)
            //    {
            //        var item = pli.ToRuntime(scope.Db, dr);
            //        if (item != null)
            //        {
            //            dr.Playlist.Items.Add(pli.ToRuntime(scope.Db, dr));
            //            var dto = new PlaylistUpdateDTO
            //            {
            //                DeviceKey = deviceKey,
            //                Items = dr.Playlist.Items.Select(x => x.ToDTO())
            //            };
            //            await SendPlaylist(dto);
            //        }
            //    }
            //}
            var dr = GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                var item = await ConvertToRuntime(dr, pli);
                if (item != null)
                {
                    dr.Playlist.Items.Add(item);
                    var dto = new PlaylistUpdateDTO
                    {
                        DeviceKey = deviceKey,
                        Items = dr.Playlist.Items.Select(x => x.ToDTO())
                    };
                    await SendPlaylist(dto);
                }
            }
        }
        public async Task CopyPlaylist(string fromDeviceKey, string toDeviceKey)
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
            this.libraryService.CopyPlaylist(fromDeviceKey, toDeviceKey);
            await ClearPlaylist(toDeviceKey);
            //using (var scope = new ScopedDbContext<MusicDb>(serviceProvider))
            //{
            //    var playlist = scope.Db.Devices.Single(x => x.KeyName == toDeviceKey).Playlist;
            //    await scope.Db.FillPlaylistForRuntime(playlist); // adds appropriate entity instqances to playlistItems
            //    foreach (var pli in playlist.Items)
            //    {
            //        await AddPlaylistItem(toDeviceKey, pli);
            //    }
            //}
            var playlist = this.libraryService.GetDevice(toDeviceKey).Playlist;
            foreach (var pli in playlist.Items)
            {
                await AddPlaylistItem(toDeviceKey, pli);
            }
        }
        public void DebugDevicePlaylist(string deviceKey)
        {
            var dr = GetDeviceRuntime(deviceKey);
            if(dr != null)
            {
                PlaylistRuntime playlist = dr.Playlist;
                log.Information(playlist.ToString());
                foreach(var item in playlist.Items ?? Enumerable.Empty<PlaylistItemRuntime>())
                {
                    log.Information($"  {item}");
                    foreach(var subItem in item.SubItems ?? Enumerable.Empty<PlaylistItemRuntime>())
                    {
                        log.Information($"    {subItem}");
                    }
                }
            }
            else
            {
                log.Error($"device {deviceKey} not found");
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
            //var pli = dr.Playlist.Items.SingleOrDefault(x => x.Sequence == position.Major);
            var pli = dr.Playlist.Items.SingleOrDefault(x => x.Position.Major == position.Major);
            if (pli != null && pli.Type == PlaylistRuntimeItemType.MultipleItems)
            {
                if (position.Minor == 0)
                {
                    log.Error($"Cannot request a position with Minor == 0 when the playlist item is of type PlaylistRuntimeItemType.MultipleItems");
                    return null;
                }
                else
                {
                    //pli = pli.SubItems.SingleOrDefault(x => x.Sequence == position.Minor);
                    pli = pli.SubItems.SingleOrDefault(x => x.Position.Minor == position.Minor);
                }
            }
            return pli;
        }
        private (PlaylistItemRuntime item, PlaylistPosition position) GetNextMinorItem(DeviceRuntime dr, bool reverse)
        {
            var sequence = dr.CurrentPosition.Minor + (reverse ? -1 : 1);
            //var majorPli = dr.Playlist.Items.Single(x => x.Sequence == dr.CurrentPosition.Major);            
            var majorPli = dr.Playlist.Items.Single(x => x.Position.Major == dr.CurrentPosition.Major);
            //var nextMinorPli = majorPli.SubItems.SingleOrDefault(x => x.Sequence == sequence);
            var nextMinorPli = majorPli.SubItems.SingleOrDefault(x => x.Position.Minor == sequence);
            return (nextMinorPli, new PlaylistPosition(dr.CurrentPosition.Major, sequence));
        }
        private (PlaylistItemRuntime item, PlaylistPosition position) GetNextMajorItem(DeviceRuntime dr, bool reverse)
        {
            var sequence = dr.CurrentPosition.Major + (reverse ? -1 : 1);
            //var majorPli = dr.Playlist.Items.SingleOrDefault(x => x.Sequence == sequence);
            var majorPli = dr.Playlist.Items.SingleOrDefault(x => x.Position.Major == sequence);
            if (majorPli != null)
            {
                if (majorPli.Type == PlaylistRuntimeItemType.MultipleItems)
                {
                    // next major item contains minor items
                    //var nextMinorPli = majorPli.SubItems.Single(x => x.Sequence == 1);
                    var nextMinorPli = majorPli.SubItems.Single(x => x.Position.Minor == 1);
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
                    //var majorPli = dr.Playlist.Items.Single(x => x.Sequence == dr.CurrentPosition.Major);
                    var majorPli = dr.Playlist.Items.Single(x => x.Position.Major == dr.CurrentPosition.Major);
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
                //using (var scope = new ScopedDbContext<MusicDb>(serviceProvider))
                //{
                //    DeviceRuntime dr = null;
                //    lock (this.runtimeDevices)
                //    {
                //        if (GetDeviceRuntime(device.KeyName) == null)
                //        {
                //            dr = new DeviceRuntime
                //            {
                //                Key = device.KeyName,
                //                Type = device.Type,
                //                DisplayName = device.DisplayName,
                //                MaxSampleRate = device.MaxSampleRate,
                //                PlayerUrl = device.PlayerUrl,
                //                CommandSequenceNumber = 0,
                //                Status = new DeviceStatus
                //                {
                //                    Key = device.KeyName,
                //                    State = PlayerStates.Idle
                //                }
                //            };
                //            dr.Playlist = device.Playlist.ToRuntime(scope.Db, dr);
                //            this.runtimeDevices.Add(dr.Key, dr);
                //            log.Information($"{device} added to run time");
                //        }
                //        else
                //        {
                //            log.Warning($"{device} already present in run time");
                //        }
                //    }
                //    if (dr != null)
                //    {
                //        await PlayerReset(dr.Key);
                //    }
                //}
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
                    dr.Playlist = await ConvertToRuntime(dr, device.Playlist);
                    if (this.runtimeDevices.TryAdd(dr.Key, dr))
                    {
                        log.Information($"{device} added to run time");
                        await PlayerReset(dr.Key);
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
        private async Task<PlaylistItemRuntime> ConvertToRuntime(DeviceRuntime dr, PlaylistItem playlistItem, int index = 0)
        {
            async Task<PlaylistItemRuntime> FromPerformance()
            {
                var performance = await this.libraryService.GetEntityAsync<Performance>(playlistItem.ItemId);
                if (performance != null)
                {
                    var movements = performance.Movements.OrderBy(m => m.MovementNumber);
                    //var subItems = await Task.WhenAll(movements.Select(async (t, i) => await ConvertToRuntime(dr, playlistItem, i)));
                    var subItems = new List<PlaylistItemRuntime>();
                    var index = 0;
                    foreach (var movement in movements)
                    {
                        var subItem = FromTrack(dr, movement, new PlaylistPosition(playlistItem.Sequence, ++index));
                        subItems.Add(subItem);
                    }
                    var plir = new PlaylistItemRuntime
                    {
                        //Id = playlistItem.Id,
                        Type = PlaylistRuntimeItemType.MultipleItems,
                        Position = new PlaylistPosition(playlistItem.Sequence, 0),
                        //Title = pli.Title,
                        Titles = new string[] { playlistItem.Title },
                        //Sequence = playlistItem.Sequence,
                        //ItemId = playlistItem.ItemId,
                        CoverArtUrl = $"lib/get/work/coverart/{movements.First().Work.Id}",
                        SubItems = subItems
                    };
                    plir.TotalTime = plir.SubItems.Sum(x => x.TotalTime);
                    plir.FormattedTotalTime = plir.TotalTime.FormatDuration();
                    return plir; 
                }
                else
                {
                    log.Warning($"{playlistItem} is not valid");
                }
                return null;
            }
            async Task<PlaylistItemRuntime> FromWork()
            {
                var work = await this.libraryService.GetEntityAsync<Work>(playlistItem.ItemId);
                if (work != null)
                {
                    var tracks = work.Tracks.OrderBy(t => t.Number);
                    //var subItems = await Task.WhenAll(tracks.Select(async (t, i) => await ConvertToRuntime(dr, playlistItem, i)));
                    var subItems = new List<PlaylistItemRuntime>();
                    var index = 0;
                    foreach (var track in tracks)
                    {
                        var subItem = FromTrack(dr, track, new PlaylistPosition(playlistItem.Sequence, ++index));
                        subItems.Add(subItem);
                    }
                    var plir = new PlaylistItemRuntime
                    {
                        //Id = playlistItem.Id,
                        Type = PlaylistRuntimeItemType.MultipleItems,
                        Position = new PlaylistPosition(playlistItem.Sequence, 0),
                        //Title = pli.Title,
                        Titles = new string[] { playlistItem.Title },
                        //Sequence = playlistItem.Sequence,
                        //ItemId = playlistItem.ItemId,
                        CoverArtUrl = $"lib/get/work/coverart/{work.Id}",
                        SubItems = subItems
                    };
                    plir.TotalTime = plir.SubItems.Sum(x => x.TotalTime);
                    plir.FormattedTotalTime = plir.TotalTime.FormatDuration();
                    return plir; 
                }
                else
                {
                    log.Warning($"{playlistItem} is not valid");
                }
                return null;
            }

            async Task<PlaylistItemRuntime> FromMusicFile()
            {
                var mf = await this.libraryService.GetEntityAsync<MusicFile>(playlistItem.MusicFileId);
                if (mf != null)
                {
                    return new PlaylistItemRuntime
                    {
                        //Id = playlistItem.Id,
                        Type = PlaylistRuntimeItemType.SingleItem,
                        Position = new PlaylistPosition(playlistItem.Sequence, 0),
                        Titles = new string[] {
                                mf.Track.Performance?.GetParentArtistsName() ?? mf.Track.Work.Artists.Select(a => a.Name).ToCSV(),
                                mf.Track.Performance?.GetParentEntityDisplayName() ?? mf.Track.Work.Name,
                                mf.Track.Title
                            },
                        //Sequence = playlistItem.Sequence,
                        //NotPlayableOnCurrentDevice = !playable,
                        NotPlayableOnCurrentDevice = !(dr.MaxSampleRate == 0 || mf.SampleRate == 0 || mf.SampleRate <= dr.MaxSampleRate),
                        //ItemId = playlistItem.ItemId,
                        MusicFileId = mf.Id,
                        AudioProperties = mf.GetAudioProperties(),
                        SampleRate = mf.SampleRate ?? 0,
                        TotalTime = mf.Duration ?? 0.0,
                        FormattedTotalTime = mf.Duration?.FormatDuration() ?? "00:00",
                        CoverArtUrl = $"lib/get/work/coverart/{mf.Track.Work.Id}"
                    };
                }
                else
                {
                    log.Warning($"{playlistItem} is not valid");
                }
                return null;
            }

            return playlistItem.Type switch
            {
                PlaylistItemType.MusicFile => await FromMusicFile(),
                PlaylistItemType.Track => await FromTrack(dr, playlistItem),
                PlaylistItemType.Work => await FromWork(),
                PlaylistItemType.Performance => await FromPerformance(),
                _ => throw new Exception($"Unexpected PlaylistItemType {playlistItem.Type}"),
            };
        }
        private async Task<PlaylistRuntime> ConvertToRuntime(DeviceRuntime dr, Playlist playlist)
        {
            async Task<List<PlaylistItemRuntime>> GetPlaylistItemRuntimes(/*DeviceRuntime dr, Playlist list*/)
            {
                var runtimeItems = new List<PlaylistItemRuntime>();
                var index = 0;
                foreach (var pli in playlist.Items)
                {
                    var runtimeItem = await ConvertToRuntime(dr, pli, index++);
                    if (runtimeItem != null)
                    {
                        runtimeItems.Add(runtimeItem);
                    }
                }

                return runtimeItems;
            }
            log.Information($"{dr} {(playlist as IIdentifier).ToIdent()}");
            // **NB** async within a select with a .WhenAll does not work with efcore dbcontext
            // because it causes multiple tasks on different threads which are not supported for dbcontext
            //var items = await Task.WhenAll(list.Items.Select(async x => await ConvertToRuntime(dr, x)));
            List<PlaylistItemRuntime> runtimeItems = await GetPlaylistItemRuntimes(/*dr, list*/);
            return new PlaylistRuntime
            {
                Id = playlist.Id,
                Name = playlist.Name,
                Type = playlist.Type,
                Items = runtimeItems
                    //.Where(x => x != null)
                    //.OrderBy(x => x.Sequence).ToList()
                    .OrderBy(x => x.Position.Major).ToList()
            };
        }

        private async Task<PlaylistItemRuntime> FromTrack(DeviceRuntime dr, PlaylistItem playlistItem)
        {
            var track = await this.libraryService.GetEntityAsync<Track>(playlistItem.ItemId);
            return FromTrack(dr, track, new PlaylistPosition(playlistItem.Sequence, 0));
        }
        private PlaylistItemRuntime FromTrack(DeviceRuntime dr, Track track, PlaylistPosition position)
        {
            var mf = track.GetBestMusicFile(dr);
            var title = track.Title;
            if(position.Minor > 0 && title.Contains(':'))
            {
                title = title.Split(':').Skip(1).First();
            }
            return new PlaylistItemRuntime
            {
                //Id = playlistItem.Id,
                Type = PlaylistRuntimeItemType.SingleItem,
                Position = position,// new PlaylistPosition(playlistItem.Sequence, index),
                Titles = new string[] {
                                track.Performance?.GetParentArtistsName() ?? track.Work.Artists.Select(a => a.Name).ToCSV(),
                                track.Performance?.GetParentEntityDisplayName() ?? track.Work.Name,
                                title //track.Title
                            },
                //Sequence = playlistItem.Sequence,
                NotPlayableOnCurrentDevice = mf == null,
                //ItemId = playlistItem.ItemId,
                MusicFileId = mf?.Id ?? 0,
                AudioProperties = mf?.GetAudioProperties(),
                SampleRate = mf?.SampleRate ?? 0,
                TotalTime = mf?.Duration ?? 0.0,
                FormattedTotalTime = mf?.Duration?.FormatDuration() ?? "00:00",
                CoverArtUrl = $"lib/get/work/coverart/{track.Work.Id}"
            };
        }
    }
}
