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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    //public interface ILibraryService
    //{
    //    Task<Device> ConfirmDevice(AudioDevice audioDevice);
    //    void CopyPlaylist(string fromDevicekey, string toDevicekey);
    //    IEnumerable<Device> GetActiveAudioDevices(IEnumerable<string> keys);
    //    IEnumerable<Device> GetAllDevices();
    //    IEnumerable<Device> GetAudioDevices(bool all);
    //    Device GetDevice(AudioDevice audioDevice);
    //    Device GetDevice(string deviceKey);
    //    IEnumerable<Device> GetDevices(IEnumerable<AudioDevice> audioDevices);
    //    IEnumerable<Device> GetDevices(IEnumerable<string> deviceKeys);
    //    Task<T> GetEntity<T>(long id) where T : EntityBase;
    //    Task ReplacePlaylistItems<T>(Playlist playlist, T entity) where T : EntityBase;
    //    Task SendArtistDeleted(long id);
    //    Task SendArtistNewOrModified(long id);
    //    Task<Device> UpdateDevice(AudioDevice audioDevice);
    //}
    //public interface ILibraryServiceScoped : ILibraryService
    //{

    //}
    
    public partial class LibraryService //: ILibraryService, ILibraryServiceScoped
    {
        private readonly IHubContext<MessageHub, IHubMessage> messageHub;
        //private readonly string connectionString;
        private readonly MusicServerOptions musicServerOptions;
        protected MusicDb musicDb;
        private readonly ILogger log;
        public LibraryService(IOptions<MusicServerOptions> serverOptions, IHubContext<MessageHub, IHubMessage> messageHub, ILogger<LibraryService> logger, MusicDb db) //: base(serverOptions, messageHub, logger)
        {
            this.musicDb = db;
            this.log = logger;
            this.musicServerOptions = serverOptions.Value;
            this.messageHub = messageHub;
        }
        //public LibraryService(IOptions<MusicServerOptions> serverOptions, IHubContext<MessageHub, IHubMessage> messageHub,
        //    ILogger<LibraryService> logger)
        //{
        //    this.log = logger;
        //    this.musicServerOptions = serverOptions.Value;
        //    this.messageHub = messageHub;
        //    //this.musicDb = db;
        //}
        public async Task SendArtistNewOrModified(long id)
        {
            try
            {
                await this.messageHub.Clients.All.SendArtistNewOrModified(id);
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }

        }
        public async Task SendArtistDeleted(long id)
        {
            try
            {
                await this.messageHub.Clients.All.SendArtistDeleted(id);
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }

        }
        //
        public async Task<T> GetEntity<T>(long id) where T : EntityBase
        {
            return await musicDb.Set<T>().FindAsync(id);
        }
        //
        public IEnumerable<Device> GetAudioDevices(bool all)
        {
            //using var musicDb = new MusicDb(connectionString);
            IEnumerable<Device> devices = musicDb.Devices.OrderBy(x => x.HostMachine)
                .ThenBy(x => x.IsDisabled)
                .ThenBy(x => x.DisplayName);
            if (!all)
            {
                devices = devices.Where(x => x.IsDisabled == false);
            }
            return devices;
        }
        public async Task<Device> ConfirmDevice(AudioDevice audioDevice)
        {
            //using var musicDb = new MusicDb(connectionString);
            //
            bool disableOnCreate(string macAddress)
            {
                var dn = musicServerOptions.DisplayNames.FirstOrDefault(x => string.Compare(x.MACAddress, audioDevice.MACAddress, true) == 0);
                return dn?.DisableOnCreate ?? false;
            }
            string GetDisplayName()
            {
                var dn = musicServerOptions.DisplayNames.FirstOrDefault(x => string.Compare(x.MACAddress, audioDevice.MACAddress, true) == 0);
                if (dn == null)
                {
                    dn = musicServerOptions.DisplayNames.FirstOrDefault(x => string.Compare(x.Name, audioDevice.Name, true) == 0);
                }
                if (dn != null)
                {
                    return dn.DisplayName;
                }
                else
                {
                    return audioDevice.Name;
                }
            }
            Device AddNewDeviceToDB()
            {

                var device = new Device
                {
                    KeyName = Guid.NewGuid().ToString().ToLower(),
                    Name = audioDevice.Name.ToLower(),
                    Type = audioDevice.Type,
                    MACAddress = audioDevice.MACAddress,
                    IsDefaultOnHost = false,
                    IsDisabled = false,
                    HostMachine = audioDevice.HostMachine.ToLower(),
                    DisplayName = GetDisplayName(),
                    Volume = 0.0F,
                    MaxSampleRate = audioDevice.Capability?.MaxSampleRate ?? 0,
                    CanReposition = audioDevice.CanReposition,
                    Playlist = new Playlist
                    {
                        Type = PlaylistType.DeviceList
                    }
                };
                device.IsDisabled = disableOnCreate(device.MACAddress);
                musicDb.Devices.Add(device);
                return device;
            }
            bool ValidatePlaylist(Device device)
            {
                var result = true;
                foreach (var item in device.Playlist.Items.ToArray())
                {
                    switch (item.Type)
                    {
                        case PlaylistItemType.MusicFile:
                            result = musicDb.MusicFiles.Find(item.MusicFileId) != null;
                            break;
                        case PlaylistItemType.Track:
                            result = musicDb.Tracks.Find(item.ItemId) != null;
                            break;
                        case PlaylistItemType.Work:
                            result = musicDb.Works.Find(item.ItemId) != null;
                            break;
                        case PlaylistItemType.Performance:
                            result = musicDb.Performances.Find(item.ItemId) != null;
                            break;
                    }
                    if (result == false)
                    {
                        device.Playlist.Items.Clear();
                        log.Warning($"Existing playlist for device {device.DisplayName} cleared as at least one item is not valid");
                        break;
                    }
                }
                return result;
            }
            //
            Device device = null;
            if (audioDevice.Type != AudioDeviceType.Browser)
            {
                device = await musicDb.Devices.SingleOrDefaultAsync(x => x.HostMachine.ToLower() == audioDevice.HostMachine.ToLower()
                    && x.MACAddress.ToLower() == audioDevice.MACAddress.ToLower());
                if (device == null)
                {
                    device = AddNewDeviceToDB();
                }
                if (device.CanReposition != audioDevice.CanReposition || device.MaxSampleRate != (audioDevice.Capability?.MaxSampleRate ?? 0))
                {
                    device.CanReposition = audioDevice.CanReposition;
                    device.MaxSampleRate = audioDevice.Capability?.MaxSampleRate ?? 0;
                }
                device.LastSeenDateTime = DateTimeOffset.Now;
                device.PlayerUrl = audioDevice.Url;
                await musicDb.SaveChangesAsync();
            }
            else
            {
                // all browser devices must have a key already (due to prior web audio registration)
                device = await musicDb.Devices.SingleOrDefaultAsync(x => x.KeyName == audioDevice.Key);
                // Note: device.LastSeenDateTime is set during web audio start
                await musicDb.SaveChangesAsync();
            }
            if (!ValidatePlaylist(device))
            {
                await musicDb.SaveChangesAsync();
            }

            log.Debug($"Confirming device {device.KeyName}, {device.DisplayName}, {device.HostMachine}, disabled =  {device.IsDisabled} ");
            return device;
        }
        public IEnumerable<Device> GetActiveAudioDevices(IEnumerable<string> keys)
        {
            //using var musicDb = new MusicDb(connectionString);
            var devices = musicDb.Devices
                .Where(x => keys.Contains(x.KeyName))
                .OrderBy(x => x.HostMachine)
                .ThenBy(x => x.IsDisabled)
                .ThenBy(x => x.DisplayName);
            log.Debug($"returning a list of {devices.Count()} active devices");
            return devices;
        }
        public Device GetDevice(AudioDevice audioDevice)
        {
            return GetDevice(audioDevice.Key);
        }
        public Device GetDevice(string deviceKey)
        {
            //using var musicDb = new MusicDb(connectionString);
            var device = musicDb.Devices.SingleOrDefault(x => x.KeyName == deviceKey);
            if (device == null)
            {
                log.Error($"device {deviceKey} not found");
            }
            return device;
        }
        public IEnumerable<Device> GetDevices(IEnumerable<AudioDevice> audioDevices)
        {
            return GetDevices(audioDevices.Select(d => d.Key));
        }
        public IEnumerable<Device> GetDevices(IEnumerable<string> deviceKeys)
        {
            //using var musicDb = new MusicDb(connectionString);
            var devices = musicDb.Devices.Where(x => deviceKeys.Contains(x.KeyName));
            if (devices.Count() != deviceKeys.Count())
            {
                foreach (var key in deviceKeys.Except(devices.Select(d => d.KeyName)))
                {
                    log.Error($"device {key} not found");
                }
            }
            return devices;
        }
        public IEnumerable<Device> GetAllDevices()
        {
            return musicDb.Devices;
        }
        public async Task<Device> UpdateDevice(AudioDevice audioDevice)
        {
            //using var musicDb = new MusicDb(connectionString);
            var device = musicDb.Devices
                .SingleOrDefault(x => x.KeyName == audioDevice.Key);
            if (device != null)
            {
                if (device.DisplayName != audioDevice.DisplayName)
                {
                    device.DisplayName = audioDevice.DisplayName;
                }
                if (device.IsDisabled != !audioDevice.Enabled)
                {
                    device.IsDisabled = !audioDevice.Enabled;
                }
                await musicDb.SaveChangesAsync();
            }
            else
            {
                log.Warning($"{audioDevice.HostMachine}, {audioDevice.DisplayName} not found ({audioDevice.Key})");
            }
            return device;
        }
        //
        public async Task ReplacePlaylistItems<T>(Playlist playlist, T entity) where T : EntityBase
        {
            playlist.Items.Clear();
            PlaylistItem pli = entity switch
            {
                MusicFile mf => CreateNewPlaylistItem(mf),
                Track t => CreateNewPlaylistItem(t),
                Work w => CreateNewPlaylistItem(w),
                Performance p => CreateNewPlaylistItem(p),
                _ => throw new Exception($"Entity type {entity.GetType().Name} is not playable"),
            };
            pli.Sequence = 1;
            playlist.Items.Add(pli);
            await musicDb.SaveChangesAsync();
        }
        public void CopyPlaylist(string fromDevicekey, string toDevicekey)
        {
            //using var musicDb = new MusicDb(connectionString);
            var from = musicDb.Devices.Single(x => x.KeyName == fromDevicekey);
            var to = musicDb.Devices.Single(x => x.KeyName == toDevicekey);
            var toRemove = to.Playlist.Items.ToArray();
            musicDb.PlaylistItems.RemoveRange(toRemove);
            foreach (var item in from.Playlist.Items)
            {
                var ni = new PlaylistItem
                {
                    Playlist = to.Playlist,
                    ItemId = item.ItemId,
                    MusicFile = item.MusicFile,
                    MusicFileId = item.MusicFileId,
                    Performance = item.Performance,
                    Sequence = item.Sequence,
                    Title = item.Title,
                    Track = item.Track,
                    Type = item.Type,
                    Work = item.Work
                };
                musicDb.PlaylistItems.Add(ni);
            }
            musicDb.SaveChanges();
        }
        //
        //protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        //{
        //    this.cancellationToken = cancellationToken;
        //    try
        //    {
        //        await Start();
        //    }
        //    catch (AggregateException ae)
        //    {
        //        foreach (var xe in ae.InnerExceptions)
        //        {
        //            log.Error($"Aggregated exception {xe.GetType().Name}, {xe.Message}");
        //        }
        //    }
        //    catch (Exception xe)
        //    {
        //        log.Error(xe);
        //    }
        //}
        //private async Task Start()
        //{
        //    log.Information($"started");
        //    while (!cancellationToken.IsCancellationRequested)
        //    {
        //        try
        //        {
        //            var ts = DateTime.Today.AddDays(1) - DateTime.Now;
        //            ts = ts.Add(TimeSpan.FromHours(3));
        //            log.Debug($"waiting for {ts.TotalMinutes} minutes");
        //            await Task.Delay(ts, cancellationToken);
        //            if (!cancellationToken.IsCancellationRequested)
        //            {
        //                this.musicDb = new MusicDb(this.connectionString);
        //                log.Information($"music db data context reset");
        //            }
        //        }
        //        catch (Exception xe)
        //        {
        //            log.Error(xe);
        //        }
        //    }
        //    log.Information($"cancellation requested");
        //}
        private PlaylistItem CreateNewPlaylistItem(MusicFile mf)
        {
            var pli = new PlaylistItem
            {
                Type = PlaylistItemType.MusicFile,
                Title = mf.Track.Title,
                ItemId = mf.Track.Id,
                MusicFileId = mf.Id
            };
            return pli;
        }
        private PlaylistItem CreateNewPlaylistItem(Track track)
        {
            var pli = new PlaylistItem
            {
                Type = PlaylistItemType.Track,
                Title = track.Title,
                ItemId = track.Id,
                MusicFileId = 0
            };
            return pli;
        }
        private PlaylistItem CreateNewPlaylistItem(Work work)
        {
            var pli = new PlaylistItem
            {
                Type = PlaylistItemType.Work,
                Title = work.Name,// mf.Track.Title,
                ItemId = work.Id,
                MusicFileId = 0// mf.Id
            };
            return pli;
        }
        private PlaylistItem CreateNewPlaylistItem(Performance performance)
        {
            //var title = performance.Composition?.Name ?? performance.RagaPerformances.Select(x => x.Raga).Single().Name;
            var pli = new PlaylistItem
            {
                Type = PlaylistItemType.Performance,
                Title = performance.GetParentEntityName(),
                ItemId = performance.Id,
                MusicFileId = 0// mf.Id
            };
            return pli;
        }
    }
    //public class ScopedLibraryService : LibraryService
    //{
    //    public ScopedLibraryService(IOptions<MusicServerOptions> serverOptions, IHubContext<MessageHub, IHubMessage> messageHub, ILogger<LibraryService> logger, MusicDb db) : base(serverOptions, messageHub, logger)
    //    {
    //        this.musicDb = db;
    //    }
    //}
    //public class SingletonLibraryService : LibraryService
    //{
    //    public SingletonLibraryService(IWebHostEnvironment environment, IConfiguration cfg, IOptions<MusicServerOptions> serverOptions, IHubContext<MessageHub, IHubMessage> messageHub, ILogger<LibraryService> logger) : base(serverOptions, messageHub, logger)
    //    {
    //        var connectionString = environment.LocaliseConnectionString(cfg.GetConnectionString("MusicDb"));
    //        this.musicDb = new MusicDb(connectionString);
    //    }
    //}
}
