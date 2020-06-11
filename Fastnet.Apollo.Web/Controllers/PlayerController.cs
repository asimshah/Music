using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Core.Web.Controllers;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Messages;
using Microsoft.AspNetCore.Hosting;
//using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web.Controllers
{
    [Route("player")]
    [ApiController]
    public class PlayerController : BaseController
    {
        private readonly MusicDb musicDb;
        private readonly PlayManager playManager;
        private readonly ILoggerFactory loggerFactory;
        private readonly MusicServerOptions musicServerOptions;
        //private readonly IHubContext<PlayHub, IHubMessage> playHub;
        public PlayerController(MusicDb mdb,
            IOptions<MusicServerOptions> serverOptions, PlayManager pm,
            ILoggerFactory loggerFactory, ILogger<PlayerController> logger, /*IHostingEnvironment env,*/ IWebHostEnvironment env) : base(logger, env)
        {
            this.musicServerOptions = serverOptions.Value;
            playManager = pm;// schedulerService.GetRealtimeTask<PlayManager>();// (hs as SchedulerService).GetRealtimeTask<PlayManager>();
            this.musicDb = mdb;
            this.loggerFactory = loggerFactory;
        }
        [HttpGet("get/device/{deviceKey}/status")]
        public IActionResult GetDeviceStatus(string deviceKey)
        {            
            DeviceRuntime dr = this.playManager.GetDeviceRuntime(deviceKey);
            if(dr != null)
            {
                return SuccessResult(dr.Status.ToDTO(dr));
            }
            else
            {
                log.Error($"device with key {deviceKey} not found");
                return ErrorResult($"device with key {deviceKey} not found");
            }
        }
        [HttpGet("get/device/{deviceKey}/playlist")]
        public IActionResult GetDevicePlaylist(string deviceKey)
        {
            var dr = this.playManager.GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                if(dr.Playlist != null)
                {
                    //var list = dr.Playlist.Items.Select(x => x.ToDTO());
                    return SuccessResult(dr.Playlist.Items.Select(x => x.ToDTO()));
                }
                else
                {
                    log.Error($"device with key {deviceKey} has no playlist");
                    return ErrorResult($"device with key {deviceKey} has no playlist");
                }
            }
            else
            {
                log.Error($"device {deviceKey} not in run time");
                return ErrorResult($"device {deviceKey} not in run time");
            }
        }
        /// <summary>
        /// </summary>
        /// <returns></returns>
        [HttpPost("confirm/device")]
        public async Task<IActionResult> ConfirmDevice()
        {
            Device device = null;
            var deviceToConfirm = await this.Request.FromBody<AudioDevice>();
            if (deviceToConfirm.Type != AudioDeviceType.Browser)
            {
                device = await musicDb.Devices.SingleOrDefaultAsync(x => x.HostMachine.ToLower() == deviceToConfirm.HostMachine.ToLower()
                    && x.MACAddress.ToLower() == deviceToConfirm.MACAddress.ToLower());
                    //&& x.Name.ToLower() == deviceToConfirm.Name.ToLower()); ; 
                if (device == null)
                {
                    device = AddNewDeviceToDB(deviceToConfirm);
                }
                if (device.CanReposition != deviceToConfirm.CanReposition || device.MaxSampleRate != (deviceToConfirm.Capability?.MaxSampleRate ?? 0))
                {
                    device.CanReposition = deviceToConfirm.CanReposition;
                    device.MaxSampleRate = deviceToConfirm.Capability?.MaxSampleRate ?? 0;
                }
                device.LastSeenDateTime = DateTimeOffset.Now;
                device.PlayerUrl = deviceToConfirm.Url;
                await musicDb.SaveChangesAsync();
            }
            else
            {
                // all browser devices must have a key already (due to prior web audio registration)
                device = await musicDb.Devices.SingleOrDefaultAsync(x => x.KeyName == deviceToConfirm.Key);
                // Note: device.LastSeenDateTime is set during web audio start
                await musicDb.SaveChangesAsync();
            }
            if(!ValidatePlaylist(device))
            {
                await musicDb.SaveChangesAsync();
            }
            log.Debug($"Confirming device {device.KeyName}, {device.DisplayName}, {device.HostMachine}, disabled =  {device.IsDisabled} ");
            await SyncDeviceWithPlayManager(device);
            return SuccessResult(device.ToDTO());
        }
        [HttpGet("get/devices/{all}")]
        public IActionResult GetAudioDevices(bool all)
        {
            IEnumerable<Device> devices = musicDb.Devices.OrderBy(x => x.HostMachine)
                .ThenBy(x => x.IsDisabled)
                .ThenBy(x => x.DisplayName);
            if (!all)
            {
                devices = devices.Where(x => x.IsDisabled == false /*&& x.IsAvailable*/);
            }
            return SuccessResult(devices.Select(x => x.ToDTO()));
        }
        [HttpGet("get/devices/active/{includeKey?}")]
        public IActionResult GetActiveAudioDevices(string includeKey = null)
        {
            try
            {
                var keys = this.playManager.GetDeviceRuntimes()
            .Where(x => x.Type != AudioDeviceType.Browser || (includeKey != null && x.Key == includeKey))
            .Select(x => x.Key);

                IEnumerable<Device> devices = musicDb.Devices
                    .Where(x => keys.Contains(x.KeyName))
                    .OrderBy(x => x.HostMachine)
                    .ThenBy(x => x.IsDisabled)
                    .ThenBy(x => x.DisplayName);
                log.Debug($"returning a list of {devices.Count()} active devices");
                return SuccessResult(devices.Select(x => x.ToDTO()));
            }
            catch (Exception xe)
            {
                log.Error(xe);
                return ErrorResult("No devices found");
            }

        }
        //[HttpGet("get/device/samplerate/{deviceKey}")]
        //public IActionResult GetDeviceSampleRate(string deviceKey)
        //{
        //    DeviceRuntime dr = this.playManager.GetDeviceRuntime(deviceKey);
        //    return SuccessResult(dr.MaxSampleRate);
        //}
        [HttpPost("update/device")]
        public async Task<IActionResult> UpdateDevice()
        {
            var dto = await this.Request.FromBody<AudioDevice>();
            try
            {
                var device = musicDb.Devices
                    .SingleOrDefault(x => x.KeyName == dto.Key);
                if (device != null)
                {
                    if (device.DisplayName != dto.DisplayName)
                    {
                        device.DisplayName = dto.DisplayName;
                    }
                    if (device.IsDisabled != !dto.Enabled)
                    {
                        device.IsDisabled = !dto.Enabled;
                    }
                    await musicDb.SaveChangesAsync();
                    await SyncDeviceWithPlayManager(device);
                    //await playManager.UpdateDeviceRuntime(device);
                }
                else
                {
                    log.Warning($"{dto.HostMachine}, {dto.DisplayName} not found ({dto.Key})");
                }
                return SuccessResult();
            }
            catch (Exception xe)
            {
                log.Error(xe);
                return ExceptionResult(xe);
            }
        }
        /// <summary>
        /// sent by all music player agents in response to a music server multicast
        /// if they think the music server has been down - the list is used to ensure the
        /// run time device information is ggod
        /// </summary>
        /// <returns></returns>
        [HttpPost("current/device/list")]
        public async Task<IActionResult> PostCurrentDeviceList()
        {
            var devices = await this.Request.FromBody<AudioDevice[]>();
            foreach (var audioDevice in devices)
            {
                var device = musicDb.Devices.SingleOrDefault(x => x.KeyName == audioDevice.Key);
                if (device == null)
                {
                    log.Error($"device {audioDevice.Key} not found");
                }
                else
                {
                    await SyncDeviceWithPlayManager(device);
                }
            }
            return SuccessResult();
        }

        [HttpGet("play/next/{deviceKey}")]
        public async Task<IActionResult> PlayNextItem(string deviceKey)
        {
            var sn =  await playManager.PlayNext(deviceKey);
            return SuccessResult(sn);
        }
        [HttpGet("copy/playlist/{fromDeviceKey}/{toDeviceKey}")]
        public async Task<IActionResult> CopyPlaylist(string fromDeviceKey, string toDeviceKey)
        {
            await playManager.CopyPlaylist(fromDeviceKey, toDeviceKey);
            //await playManager.ClearPlaylist(toDeviceKey);
            //var playlist = musicDb.Devices.Single(x => x.KeyName == toDeviceKey).Playlist;
            //await musicDb.FillPlaylistForRuntime(playlist); // adds appropriate entity instqances to playlistItems
            //foreach(var pli in playlist.Items)
            //{                
            //    await playManager.AddPlaylistItem(toDeviceKey, pli);
            //}
            await PlayNextItem(toDeviceKey);
            return SuccessResult();
        }
        [HttpGet("play/previous/{deviceKey}")]
        public async Task<IActionResult> PlayPreviousItem(string deviceKey)
        {
            var sn = await playManager.PlayNext(deviceKey, true);
            return SuccessResult(sn);
        }
        //[HttpGet("play/sequence/{deviceKey}/{major}/{minor?}")]
        //public async Task<IActionResult> PlaySequenceNumber(string deviceKey, int major, int minor = 0)
        //{
        //    var position = new PlaylistPosition(major, minor);
        //    //var sn = await playManager.PlaySequenceNumber(deviceKey, major);
        //    var sn = await playManager.PlaySequenceNumber(deviceKey, position);
        //    return SuccessResult(sn);
        //}
        [HttpGet("play/item/{deviceKey}/{major}/{minor}")]
        public async Task<IActionResult> PlayItem(string deviceKey, int major, int minor)
        {
            var position = new PlaylistPosition(major, minor);
            //var sn = await playManager.PlaySequenceNumber(deviceKey, major);
            var sn = await playManager.PlaySequenceNumber(deviceKey, position);
            return SuccessResult(sn);
        }
        [HttpGet("play/file/{deviceKey}/{musicFileId}")]
        public async Task<IActionResult> PlayMusicFile(string deviceKey, long musicFileId)
        {
            var dr = this.playManager.GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                var device = await musicDb.Devices.SingleOrDefaultAsync(x => x.KeyName == deviceKey);
                var mf = await musicDb.MusicFiles.FindAsync(musicFileId);
                var sn = await PlayEntity<MusicFile>(device, mf);
                return SuccessResult(sn);
            }
            else
            {
                log.Error($"device {deviceKey} not in run time");
                return ErrorResult($"device {deviceKey} not in run time");
            }
        }
        [HttpGet("play/track/{deviceKey}/{trackId}")]
        public async Task<IActionResult> PlayTrack(string deviceKey, long trackId)
        {
            var dr = this.playManager.GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                var device = await musicDb.Devices.SingleOrDefaultAsync(x => x.KeyName == deviceKey);
                var track = await this.musicDb.Tracks.FindAsync(trackId);
                //var mf = track.GetBestMusicFile(); // should I do this here or later at the time that we are about to play
                var sn = await PlayEntity<Track>(device, track);
                return SuccessResult(sn);
            }
            else
            {
                log.Error($"device {deviceKey} not in run time");
                return ErrorResult($"device {deviceKey} not in run time");
            }
        }
        [HttpGet("play/work/{deviceKey}/{workId}")]
        public async Task<IActionResult> PlayWork(string deviceKey, long workId)
        {
            var dr = this.playManager.GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                var device = await musicDb.Devices.SingleOrDefaultAsync(x => x.KeyName == deviceKey);
                var work = await musicDb.Works.FindAsync(workId);
                var sn = await PlayEntity<Work>(device, work);
                return SuccessResult(sn);
                //var playlist = device.Playlist;
                //playlist.Items.Clear();
                ////var mf = await musicDb.MusicFiles.FindAsync(musicFileId);
                ////var pli = CreateNewPlaylistItem(mf);
                //var work = await musicDb.Works.FindAsync(workId);
                //var pli = CreateNewPlaylistItem(work);
                //playlist.AddPlaylistItem(pli);
                //await musicDb.SaveChangesAsync();
                //await musicDb.FillPlaylistItemForRuntime(pli);
                //await playManager.ClearPlaylist(deviceKey);
                //await playManager.AddPlaylistItem(deviceKey, pli);
                //var sn = await playManager.PlayNext(deviceKey);
                //return SuccessResult(sn);
            }
            else
            {
                log.Error($"device {deviceKey} not in run time");
                return ErrorResult($"device {deviceKey} not in run time");
            }
        }
        [HttpGet("play/performance/{deviceKey}/{performanceId}")]
        public async Task<IActionResult> PlayPerformance(string deviceKey, long performanceId)
        {
            var dr = this.playManager.GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                var device = await musicDb.Devices.SingleOrDefaultAsync(x => x.KeyName == deviceKey);
                var performance = await musicDb.Performances.FindAsync(performanceId);
                var sn = await PlayEntity<Performance>(device, performance);
                return SuccessResult(sn);
            }
            else
            {
                log.Error($"device {deviceKey} not in run time");
                return ErrorResult($"device {deviceKey} not in run time");
            }
        }
        [HttpGet("queue/file/{deviceKey}/{musicFileId}")]
        public async Task<IActionResult> QueueMusicFile(string deviceKey, long musicFileId)
        {
            var device = await musicDb.Devices.SingleOrDefaultAsync(x => x.KeyName == deviceKey);
            var mf = await musicDb.MusicFiles.FindAsync(musicFileId);
            await QueueEntity<MusicFile>(device, mf);
            return SuccessResult();
        }
        [HttpGet("queue/track/{deviceKey}/{trackId}")]
        public async Task<IActionResult> QueueTrack(string deviceKey, long trackId)
        {
            var device = await musicDb.Devices.SingleOrDefaultAsync(x => x.KeyName == deviceKey);
            var track = await this.musicDb.Tracks.FindAsync(trackId);
            //var mf = track.GetBestMusicFile(); // should I do this here or later at the time that we are about to play
            await QueueEntity<Track>(device, track);
            return SuccessResult();
        }
        [HttpGet("queue/work/{deviceKey}/{workId}")]
        public async Task<IActionResult> QueueWork(string deviceKey, long workId)
        {
            var device = await musicDb.Devices.SingleOrDefaultAsync(x => x.KeyName == deviceKey);
            var work = await musicDb.Works.FindAsync(workId);
            await QueueEntity<Work>(device, work);
            return SuccessResult();
            //var playlist = device.Playlist;

            //var pli = CreateNewPlaylistItem(work);
            //playlist.AddPlaylistItem(pli);
            //await musicDb.SaveChangesAsync();
            ////now we need to send out a playlist update ..
            //await musicDb.FillPlaylistItemForRuntime(pli);
            //await playManager.AddPlaylistItem(deviceKey, pli);
            //return SuccessResult();
        }
        [HttpGet("queue/performance/{deviceKey}/{performanceId}")]
        public async Task<IActionResult> QueuePerformance(string deviceKey, long performanceId)
        {
            var device = await musicDb.Devices.SingleOrDefaultAsync(x => x.KeyName == deviceKey);
            var performance = await musicDb.Performances.FindAsync(performanceId);
            await QueueEntity<Performance>(device, performance);
            return SuccessResult();
            //var playlist = device.Playlist;

            //var pli = CreateNewPlaylistItem(work);
            //playlist.AddPlaylistItem(pli);
            //await musicDb.SaveChangesAsync();
            ////now we need to send out a playlist update ..
            //await musicDb.FillPlaylistItemForRuntime(pli);
            //await playManager.AddPlaylistItem(deviceKey, pli);
            //return SuccessResult();
        }
        private async Task QueueEntity<T>(Device device, T entity)
        {
            var playlist = device.Playlist;
            //var pli = CreateNewPlaylistItem(entity);
            PlaylistItem pli = null;
            switch (entity)
            {
                case MusicFile mf:
                    pli = CreateNewPlaylistItem(mf);
                    break;
                case Track t:
                    pli = CreateNewPlaylistItem(t);
                    break;
                case Work w:
                    pli = CreateNewPlaylistItem(w);
                    break;
                case Performance p:
                    pli = CreateNewPlaylistItem(p);
                    break;
                default:
                    throw new Exception($"Entity type {entity.GetType().Name} is not playable");
            }
            playlist.AddPlaylistItem(pli);
            await musicDb.SaveChangesAsync();
            //now we need to send out a playlist update ..
            await musicDb.FillPlaylistItemForRuntime(pli);
            await playManager.AddPlaylistItem(device.KeyName, pli);
            //return SuccessResult();
        }
        [HttpGet("togglePlayPause/{deviceKey}")]
        public async Task<IActionResult> TogglePlayPause(string deviceKey)
        {
            var dr = this.playManager.GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                int sn = 0;
                switch(dr.Status.State)
                {
                    case PlayerStates.Paused:
                    case PlayerStates.Playing:
                        sn = await playManager.TogglePlayPause(deviceKey);
                        break;
                    case PlayerStates.Idle:
                    case PlayerStates.SilentIdle:
                        sn = await playManager.PlayNext(deviceKey);// await PlayNextItem(deviceKey);
                        break;
                    default:
                        log.Warning($"{dr.DisplayName} TogglePlayPause in state {dr.Status.State}");
                        break;
                }

                return SuccessResult(sn);
            }
            else
            {
                log.Error($"device {deviceKey} not in run time");
                return ErrorResult($"device {deviceKey} not in run time");
            }
        }
        [HttpGet("reposition/{deviceKey}/{position}")]
        public async Task<IActionResult> Reposition(string deviceKey, float position)
        {
            // position is a decimal
            var sn = await playManager.SetPosition(deviceKey, position);
            return SuccessResult(sn);
        }
        [HttpGet("skip/forward/{deviceKey}")]
        public async Task<IActionResult> SkipForward(string deviceKey)
        {
            var sn = await playManager.PlayNext(deviceKey);// await playManager.TogglePlayPause(deviceKey);
            return SuccessResult(sn);
        }
        [HttpGet("skip/back/{deviceKey}")]
        public async Task<IActionResult> SkipBack(string deviceKey)
        {
            var sn = await playManager.PlayNext(deviceKey, true);// await playManager.TogglePlayPause(deviceKey);
            return SuccessResult(sn);
        }
        [HttpGet("set/volume/{deviceKey}/{level}")]
        public async Task<IActionResult> SetVolume(string deviceKey, float level)
        {
            var sn = await playManager.SetVolume(deviceKey, level);
            return SuccessResult(sn);
        }
        [HttpGet("stream/{musicFileId}")]
        public async Task<IActionResult> StreamAudio(long musicFileId)
        {
            var mf = await musicDb.MusicFiles.FindAsync(musicFileId);
            if (mf != null)
            {
                var mimeType = "audio/mpeg";
                switch (mf.Encoding)
                {
                    default:
                    case EncodingType.mp3:
                        break;
                    case EncodingType.flac:
                        mimeType = "audio/flac";
                        break;
                }
                var str = System.IO.File.OpenRead(mf.File);
                var fsr = new FileStreamResult(str, mimeType);
                fsr.EnableRangeProcessing = true;
                return fsr;
            }
            else
            {
                log.Error($"No music file with pk {musicFileId} found");
            }
            return NotFound();
        }
        [HttpGet("webaudio/start/{deviceKey}")]
        public async Task<IActionResult> StartWebAudio(string deviceKey)
        {
            var device = await musicDb.Devices.SingleOrDefaultAsync(x => x.KeyName == deviceKey);
            if (device == null)
            {
                var ipAddress = this.Request.HttpContext.GetRemoteIPAddress();
                //var hostMachine = ipAddress;
                if(ipAddress == "::1")
                {
                    ipAddress = NetInfo.GetLocalIPAddress().ToString();
                    //hostMachine = Environment.MachineName.ToLower();
                }
                device = new Device
                {
                    KeyName = deviceKey,
                    Name = $"browser audio on {ipAddress}",
                    Type = AudioDeviceType.Browser,
                    MACAddress = deviceKey,
                    IsDefaultOnHost = false,
                    IsDisabled = false,
                    HostMachine =  ipAddress, //this.Request.HttpContext.GetRemoteIPAddress(),
                    DisplayName = "(local audio)",
                    Volume = 0.0F,
                    MaxSampleRate = 0,
                    CanReposition = true,
                    Playlist = new Playlist
                    {
                        Type = PlaylistType.DeviceList
                    }
                };
                await musicDb.Devices.AddAsync(device);
            }
            device.LastSeenDateTime = DateTimeOffset.Now;
            await musicDb.SaveChangesAsync();
            await SyncDeviceWithPlayManager(device);
            return SuccessResult(device.ToDTO());
        }
        [HttpPost("webaudio/device/status")]
        public async Task<IActionResult> WebAudioDeviceStatus()
        {
            var ds = await this.Request.FromBody<DeviceStatus>();
            await this.playManager.OnDeviceStatus(ds);
            return SuccessResult();
        }
        //
        [HttpGet("reset/devices")]
        public async Task<IActionResult> ResetAllDevices()
        {
            foreach(var device in musicDb.Devices.ToArray())
            {
                if(device.Playlist != null)
                {
                    musicDb.Playlists.Remove(device.Playlist);
                }
                musicDb.Devices.Remove(device);
                log.Information($"device {device.KeyName}, type {device.Type.ToString()}, {device.DisplayName} removed");
            }
            await musicDb.SaveChangesAsync();
            return SuccessResult();
        }
        //
        [HttpGet("get/all/playlists")]
        public IActionResult GetAllUserPlaylistNames()
        {
            //var list = musicDb.Playlists.Where(x => x.Type == PlaylistType.UserCreated);
            //return SuccessResult(list.Select(x => x.Name));
            return SuccessResult(new string[] {
                "alpha",
                "beta",
                "gamma",
                "delta",
            });
        }
        //
        private async Task<Device> SyncAudioDeviceWithDatabase(AudioDevice audioDevice/*, string hostName, string playerUrl*/)
        {
            DateTimeOffset lastSeenDate = DateTimeOffset.Now;
            var device = musicDb.Devices.SingleOrDefault(x => string.Compare(x.Name, audioDevice.Name, true) == 0
                && string.Compare(x.HostMachine, audioDevice.HostMachine, true) == 0);
            Debug.Assert(device != null);
            await SyncDeviceWithPlayManager(device);
            //await musicDb.FillPlaylistForRuntime(device.Playlist);
            //await playManager.UpdateDeviceRuntime(device);
            return device;
        }
        //private void SetBrowserDeviceName(AudioDevice audioDevice)
        //{
        //    var bn = musicServerOptions.DisplayNames.FirstOrDefault(x => string.Compare(x.Name, audioDevice.Name, true) == 0);
        //    if(bn != null)
        //    {
        //        audioDevice.DisplayName = bn.DisplayName;
        //    }
        //    else
        //    {
        //        audioDevice.DisplayName = audioDevice.Name;
        //    }
        //    return;
        //}
        private bool ValidatePlaylist(Device device)
        {
            var result = true;
            foreach(var item in device.Playlist.Items.ToArray())
            {
                switch(item.Type)
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
                if(result == false)
                {
                    device.Playlist.Items.Clear();
                    log.Warning($"Existing playlist for device {device.DisplayName} cleared as at least one item is not valid");
                    break;
                }
            }
            return result;
        }
        //private string GetDisplayName(AudioDevice audioDevice)
        //{
        //    var bn = musicServerOptions.DisplayNames.FirstOrDefault(x => string.Compare(x.Name, audioDevice.Name, true) == 0);
        //    if (bn != null)
        //    {
        //        return bn.DisplayName;
        //    }
        //    else
        //    {
        //        return audioDevice.Name;
        //    }
        //}
        private Device AddNewDeviceToDB(AudioDevice audioDevice/*, string hostName, string name, AudioDeviceType type, string macaddress*/)
        {
            bool disableOnCreate(string macAddress)
            {
                var dn = musicServerOptions.DisplayNames.FirstOrDefault(x => string.Compare(x.MACAddress, audioDevice.MACAddress, true) == 0);
                return dn?.DisableOnCreate ?? false;
            }
            string GetDisplayName()
            {
                var dn = musicServerOptions.DisplayNames.FirstOrDefault(x => string.Compare(x.MACAddress, audioDevice.MACAddress, true) == 0);
                if(dn == null)
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
                MaxSampleRate = audioDevice.Capability?.MaxSampleRate ??  0,
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
        private async Task<int> PlayEntity<T>(Device device, T entity)
        {
            var playlist = device.Playlist;
            playlist.Items.Clear();
            playlist.Items.Clear();
            PlaylistItem pli = null;
            switch (entity)
            {
                case MusicFile mf:
                    pli = CreateNewPlaylistItem(mf);
                    break;
                case Track t:
                    pli = CreateNewPlaylistItem(t);
                    break;
                case Work w:
                    pli = CreateNewPlaylistItem(w);
                    break;
                case Performance p:
                    pli = CreateNewPlaylistItem(p);
                    break;
                default:
                    throw new Exception($"Entity type {entity.GetType().Name} is not playable");
            }
            //var pli = CreateNewPlaylistItem(entity);
            playlist.AddPlaylistItem(pli);
            await musicDb.SaveChangesAsync();
            await musicDb.FillPlaylistItemForRuntime(pli);
            await playManager.ClearPlaylist(device.KeyName);
            await playManager.AddPlaylistItem(device.KeyName, pli);
            return await playManager.PlayNext(device.KeyName);
        }
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
        /// <summary>
        /// called from ConfirmDevice() (.. from an agent) or a user edit of the device
        /// fills out runtime playlist info and syncs with the play manager
        /// q1? shoudl playlist be created/validated here??
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        private async Task SyncDeviceWithPlayManager(Device device)
        {
            await musicDb.FillPlaylistForRuntime(device.Playlist);
            await playManager.UpdateDeviceRuntime(device);
        }

    }

}