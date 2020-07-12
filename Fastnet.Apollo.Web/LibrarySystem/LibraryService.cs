using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Messages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{

    public partial class LibraryService //: ILibraryService, ILibraryServiceScoped
    {
        #region Private Fields
        private readonly MusicDb musicDb;
        private readonly ILogger log;
        private readonly IHubContext<MessageHub, IHubMessage> messageHub;
        //private readonly string connectionString;
        private readonly MusicServerOptions musicServerOptions;

        #endregion Private Fields

        #region Public Constructors

        public LibraryService(IOptions<MusicServerOptions> serverOptions, IHubContext<MessageHub, IHubMessage> messageHub, ILogger<LibraryService> logger, MusicDb db) //: base(serverOptions, messageHub, logger)
        {
            this.musicDb = db;
            this.log = logger;
            this.musicServerOptions = serverOptions.Value;
            this.messageHub = messageHub;
        }

        #endregion Public Constructors

        #region Public Methods

        #region playlist-methods

        public async Task<PlaylistItem> AddPlaylistItemAsync<T>(Playlist playlist, T entity) where T : EntityBase
        {
            try
            {
                PlaylistItem pli = entity switch
                {
                    MusicFile mf => CreateNewPlaylistItem(mf),
                    Track t => CreateNewPlaylistItem(t),
                    Work w => CreateNewPlaylistItem(w),
                    Performance p => CreateNewPlaylistItem(p),
                    _ => throw new Exception($"Entity type {entity.GetType().Name} is not playable"),
                };
                pli.Sequence = playlist.Items.Count() + 1;
                playlist.Items.Add(pli);
                playlist.LastModified = DateTimeOffset.Now;
                await musicDb.SaveChangesAsync();
                return pli;
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }
            return null;
        }
        //public void AddPlaylistItem(ExtendedPlaylist epl, PlaylistItem item)
        //{
        //    var epli = CreateExtendedPlaylistItem(item);
        //    epl.AddItem(epli);
        //}
        public async Task ChangeDevicePlaylist(string deviceKey, Playlist playlist)
        {
            var device = this.musicDb.Devices.SingleOrDefault(d => d.KeyName == deviceKey);
            if (device == null)
            {
                throw new Exception($"device {deviceKey} not found");
            }
            if (device.Playlist.Id != playlist.Id)
            {
                Playlist toBeDeleted = null;
                if (device.Playlist.Type == PlaylistType.DeviceList)
                {
                    toBeDeleted = device.Playlist;

                }
                // **ATTENTION** 27Jun2020
                // device.playlist is a required relationship and
                // for some reason EFCORE will mark device as deleted (or otherwise
                // throw an error (later) - depending on the configured DeleteBeviour)
                // when the playlist entity is deleted EVEN THOUGH the property has been
                // reset to another playlist!!!!
                // the only way I have found around this is to reset the property and then
                // save to the database - then the old playlist can be deleted safely!
                // hence the two successive SaveChanges calls
                device.Playlist = playlist;
                await musicDb.SaveChangesAsync();
                if (toBeDeleted != null)
                {
                    DeletePlaylist(toBeDeleted);
                }
                var deviceState = musicDb.Entry(device).State;
                await musicDb.SaveChangesAsync();
            }
        }
        public async Task<Playlist> CreateDevicePlaylistAsync(string deviceKey, IEnumerable<PlaylistItem> playlistItems = null)
        {
            playlistItems ??= Enumerable.Empty<PlaylistItem>();
            var device = this.musicDb.Devices.SingleOrDefault(d => d.KeyName == deviceKey);
            if (device == null)
            {
                throw new Exception($"device {deviceKey} not found");
            }
            if (device.Playlist.Type == PlaylistType.DeviceList)
            {
                // we can reuse the current playlist
                device.Playlist.Items.Clear();
            }
            else
            {
                device.Playlist = new Playlist
                {
                    Type = PlaylistType.DeviceList,
                    Name = string.Empty,
                    LastModified = DateTimeOffset.Now,
                };
                musicDb.Playlists.Add(device.Playlist);
                await musicDb.SaveChangesAsync();
            }
            foreach (var item in playlistItems)
            {
                item.Playlist = device.Playlist;
                device.Playlist.Items.Add(item);
            }

            var playlistState = musicDb.Entry(device.Playlist).State;
            var deviceState = musicDb.Entry(device).State;
            await musicDb.SaveChangesAsync();
            return device.Playlist;
        }
        public ExtendedPlaylist CreateExtendedPlaylist(Playlist playlist)
        {
            return ExtendedPlaylist.Create(playlist, this);
        }
        public ExtendedPlaylistItem CreateExtendedPlaylistItem(PlaylistItem item)
        {
            ExtendedPlaylistItem epli = null;
            switch (item.Type)
            {
                case PlaylistItemType.MusicFile:
                    epli = new PlaylistMusicFileItem(item, this);
                    break;
                case PlaylistItemType.Track:
                    epli = new PlaylistTrackItem(item, this);
                    break;
                case PlaylistItemType.Work:
                    epli = new PlaylistWorkItem(item, this);
                    break;
                case PlaylistItemType.Performance:
                    epli = new PlaylistPerformanceItem(item, this);
                    break;
            }
            return epli;
        }
        public async Task<Playlist> CreateUserPlaylist(string name, IEnumerable<PlaylistItem> playlistItems = null)
        {
            playlistItems ??= Enumerable.Empty<PlaylistItem>();
            var playlist = new Playlist
            {
                Type = PlaylistType.UserCreated,
                Name = name,
                LastModified = DateTimeOffset.Now,
            };
            foreach (var item in playlistItems)
            {
                item.Playlist = playlist;
                playlist.Items.Add(item);
            }
            musicDb.Playlists.Add(playlist);
            await musicDb.SaveChangesAsync();
            return playlist;
        }
        public async Task DeletePlaylist(long playlistId)
        {
            var playlist = await musicDb.Playlists.FindAsync(playlistId);
            if (playlist.Type != PlaylistType.DeviceList)
            {
                DeletePlaylist(playlist);
                await musicDb.SaveChangesAsync();
            }

        }
        public void DeletePlaylist(Playlist pl)
        {
            // *NB* DO NOT save to db here as it can lead to a device being deleted! (because the fk is not nullable, I think ... ???
            var items = pl.Items.ToArray();
            musicDb.PlaylistItems.RemoveRange(items);
            musicDb.Playlists.Remove(pl);
            //await musicDb.SaveChangesAsync();
            log.Information($"{pl.ToIdent()} {pl.Type}{(pl.Type == PlaylistType.UserCreated ? " " + pl.Name : "")} deleted");
        }
        public async Task<Playlist> FindPlaylist(string name)
        {
            return await this.musicDb.Playlists.SingleOrDefaultAsync(x => x.Name.ToLower() == name.ToLower());
        }
        public IEnumerable<Playlist> GetAllPlaylists()
        {
            return musicDb.Playlists;
        }
        public async Task UpdatePlaylistItems(Playlist playlist, IEnumerable<PlaylistItem> items)
        {
            playlist.Items.Clear();
            foreach (var item in items)
            {
                playlist.Items.Add(item);
            }
            await musicDb.SaveChangesAsync();
        }
        #endregion playlist-methods

        #region device-methods
        public async Task<Device> ConfirmDeviceAsync(AudioDevice audioDevice)
        {
            //
            bool disableOnCreate()
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
                    //device = AddNewDeviceToDB();
                    device = await CreateNewDeviceAsync(Guid.NewGuid().ToString(), audioDevice.Type, audioDevice.Name, GetDisplayName(), audioDevice.MACAddress,
                        audioDevice.HostMachine, audioDevice.Capability, audioDevice.CanReposition, disableOnCreate());
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
            //else
            //{
            //    // all browser devices must have a key already (due to prior web audio registration)
            //    device = await musicDb.Devices.SingleOrDefaultAsync(x => x.KeyName == audioDevice.Key);
            //    // Note: device.LastSeenDateTime is set during web audio start
            //    await musicDb.SaveChangesAsync();
            //}
            if (!ValidatePlaylist(device))
            {
                await musicDb.SaveChangesAsync();
            }

            log.Debug($"Confirming device {device.KeyName}, {device.DisplayName}, {device.HostMachine}, disabled =  {device.IsDisabled} ");
            return device;
        }
        public async Task<Device> CreateWebAudioDevice(string deviceKey, string ipAddress)
        {
            deviceKey = deviceKey.ToLower();
            if (ipAddress == "::1")
            {
                ipAddress = NetInfo.GetLocalIPAddress().ToString();
            }
            var device = await CreateNewDeviceAsync(deviceKey, AudioDeviceType.Browser, $"browser audio on {ipAddress}", "(local audio)",
                deviceKey, ipAddress, null, true, false);
            await musicDb.SaveChangesAsync();
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
        public IEnumerable<Device> GetAllDevices()
        {
            return musicDb.Devices;
        }
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
        public async Task<T> GetEntityAsync<T>(long id) where T : EntityBase
        {
            return await musicDb.Set<T>().FindAsync(id);
        }
        public async Task MarkAsSeenAsync(Device device)
        {
            device.LastSeenDateTime = DateTimeOffset.Now;
            await musicDb.SaveChangesAsync();
        }
        public async Task ResetAllDevicesAsync()
        {
            foreach (var device in musicDb.Devices.ToArray())
            {
                if (device.Playlist != null)
                {
                    musicDb.Playlists.Remove(device.Playlist);
                }
                musicDb.Devices.Remove(device);
                log.Information($"device {device.KeyName}, type {device.Type.ToString()}, {device.DisplayName} removed");
            }
            await musicDb.SaveChangesAsync();
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
        private IEnumerable<Device> GetDevices(IEnumerable<string> deviceKeys)
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
        #endregion device-methods

        #region hub-messages
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
        #endregion hub-messages

        #endregion Public Methods

        #region Private Methods
        private async Task<Device> CreateNewDeviceAsync(string deviceKey, AudioDeviceType type, string name, string displayName, string physAddress,
                    string hostMachine, AudioCapability capability, bool canReposition, bool isDisabled)
        {
            var device = new Device
            {
                KeyName = deviceKey,// Guid.NewGuid().ToString().ToLower(),
                Name = name.ToLower().Trim(),// audioDevice.Name.ToLower(),
                Type = type, //audioDevice.Type,
                MACAddress = physAddress.ToLower().Trim(), //audioDevice.MACAddress,
                IsDefaultOnHost = false,
                IsDisabled = isDisabled,
                HostMachine = hostMachine.ToLower().Trim(),// audioDevice.HostMachine.ToLower(),
                DisplayName = displayName,// GetDisplayName(),
                Volume = 0.0F,
                MaxSampleRate = capability?.MaxSampleRate ?? 0,// audioDevice.Capability?.MaxSampleRate ?? 0,
                CanReposition = canReposition,// audioDevice.CanReposition,
                Playlist = CreateNewPlaylistInternal(PlaylistType.DeviceList)
            };
            await musicDb.Devices.AddAsync(device);
            return device;
        }
        private Playlist CreateNewPlaylistInternal(PlaylistType type, string name = null)
        {
            return new Playlist
            {
                Type = PlaylistType.DeviceList,
                Name = name ?? string.Empty,
                LastModified = DateTimeOffset.Now
            };
        }
        //public void CopyPlaylist(string fromDevicekey, string toDevicekey)
        //{
        //    //using var musicDb = new MusicDb(connectionString);
        //    var from = musicDb.Devices.Single(x => x.KeyName == fromDevicekey);
        //    var to = musicDb.Devices.Single(x => x.KeyName == toDevicekey);
        //    var toRemove = to.Playlist.Items.ToArray();
        //    musicDb.PlaylistItems.RemoveRange(toRemove);
        //    foreach (var item in from.Playlist.Items)
        //    {
        //        var ni = new PlaylistItem
        //        {
        //            Playlist = to.Playlist,
        //            ItemId = item.ItemId,
        //            //MusicFile = item.MusicFile,
        //            MusicFileId = item.MusicFileId,
        //            //Performance = item.Performance,
        //            Sequence = item.Sequence,
        //            Title = item.Title,
        //            //Track = item.Track,
        //            Type = item.Type,
        //            //Work = item.Work
        //        };
        //        musicDb.PlaylistItems.Add(ni);
        //    }
        //    musicDb.SaveChanges();
        //}
        ///// <summary>
        ///// makes the current DeviceList playlist into a UserCreated one and names it
        ///// </summary>
        ///// <param name="deviceKey"></param>
        ///// <param name="name"></param>
        ///// <returns></returns>
        //public async Task<Device> SaveNewPlaylistAsync(string deviceKey, string name)
        //{
        //    var device = musicDb.Devices.Single(x => x.KeyName == deviceKey);
        //    var playlist = device.Playlist;
        //    playlist.Name = name;
        //    playlist.Type = PlaylistType.UserCreated;
        //    await musicDb.SaveChangesAsync();
        //    return device;
        //}
        //
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
                Title = performance.GetParentEntityDisplayName(),
                ItemId = performance.Id,
                MusicFileId = 0// mf.Id
            };
            return pli;
        }
        #endregion Private Methods
    }
}
