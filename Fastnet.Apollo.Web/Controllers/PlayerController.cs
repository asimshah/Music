using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Core.Web.Controllers;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Messages;
using Microsoft.AspNetCore.Hosting;
//using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web.Controllers
{
    //[ServiceFilter(typeof(WebServiceCallTrace))]
    [Route("player")]
    [ApiController]
    public class PlayerController : BaseController
    {
        private readonly PlayManager playManager;
        private readonly LibraryService libraryService;
        private readonly MusicServerOptions musicServerOptions;
        public PlayerController(LibraryService libraryService,
            IOptions<MusicServerOptions> serverOptions, PlayManager pm,
            ILogger<PlayerController> logger, IWebHostEnvironment env) : base(logger, env)
        {
            this.musicServerOptions = serverOptions.Value;
            playManager = pm;
            this.libraryService = libraryService;// as ILibraryService;
        }
        [HttpGet("get/device/{deviceKey}/status")]
        public IActionResult GetDeviceStatus(string deviceKey)
        {
            DeviceRuntime dr = this.playManager.GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                return SuccessResult(dr.Status.ToDTO(dr));
            }
            else
            {
                log.Error($"device with key {deviceKey} not found");
                return ErrorResult($"device with key {deviceKey} not found");
            }
        }
        [HttpGet("send/device/{deviceKey}/status")]
        public async Task<IActionResult> SendDeviceStatus(string deviceKey)
        {
            DeviceRuntime dr = this.playManager.GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                await this.playManager.SendDeviceStatus(dr);
                return SuccessResult();
            }
            else
            {
                log.Error($"device with key {deviceKey} not found");
                return ErrorResult($"device with key {deviceKey} not found");
            }
        }
        [HttpGet("send/device/{deviceKey}/playlist")] 
        public async Task<IActionResult> SendDevicePlaylist(string deviceKey)
        {
            await this.playManager.SendDevicePlaylistAsync(deviceKey);
            return SuccessResult();
        }
        /// <summary>
        /// </summary>
        /// <returns></returns>
        [HttpPost("confirm/device")]
        public async Task<IActionResult> ConfirmDevice()
        {
            var deviceToConfirm = await this.Request.FromBody<AudioDevice>();
            Device device = await this.libraryService.ConfirmDeviceAsync(deviceToConfirm);
            await SyncDeviceWithPlayManager(device);
            return SuccessResult(device.ToDTO());
        }
        [HttpGet("get/devices/{all}")]
        public IActionResult GetAudioDevices(bool all)
        {
            var devices = this.libraryService.GetAudioDevices(all);
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
                var devices = this.libraryService.GetActiveAudioDevices(keys);
                return SuccessResult(devices.Select(x => x.ToDTO()));
            }
            catch (Exception xe)
            {
                log.Error(xe);
                return ErrorResult("No devices found");
            }
        }
        [HttpPost("update/device")]
        public async Task<IActionResult> UpdateDevice()
        {
            var dto = await this.Request.FromBody<AudioDevice>();
            var device = await this.libraryService.UpdateDevice(dto);
            if (device == null)
            {
                return ErrorResult($"Device {dto.Key} not found");
            }
            await SyncDeviceWithPlayManager(device);
            return SuccessResult();
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
            var audioDevices = await this.Request.FromBody<AudioDevice[]>();
            var devices = this.libraryService.GetDevices(audioDevices);
            foreach (var device in devices)
            {
                await SyncDeviceWithPlayManager(device);
            }
            return SuccessResult();
        }
        [HttpGet("play/next/{deviceKey}")]
        public async Task<IActionResult> PlayNextItem(string deviceKey)
        {
            var sn = await playManager.PlayNextAsync(deviceKey);
            return SuccessResult(sn);
        }
        [HttpGet("play/previous/{deviceKey}")]
        public async Task<IActionResult> PlayPreviousItem(string deviceKey)
        {
            var sn = await playManager.PlayNextAsync(deviceKey, true);
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
            var sn = await playManager.PlaySequenceNumberAsync(deviceKey, position);
            return SuccessResult(sn);
        }
        [HttpGet("play/file/{deviceKey}/{musicFileId}")]
        public async Task<IActionResult> PlayMusicFile(string deviceKey, long musicFileId)
        {
            var dr = this.playManager.GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                var device = this.libraryService.GetDevice(deviceKey);
                var mf = await this.libraryService.GetEntityAsync<MusicFile>(musicFileId);
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
                var device = this.libraryService.GetDevice(deviceKey);
                var track = await this.libraryService.GetEntityAsync<Track>(trackId);
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
            {;
                var device = this.libraryService.GetDevice(deviceKey);
                var work = await this.libraryService.GetEntityAsync<Work>(workId);
                var sn = await PlayEntity<Work>(device, work);
                return SuccessResult(sn);
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
                var device = this.libraryService.GetDevice(deviceKey);
                var performance = await this.libraryService.GetEntityAsync<Performance>(performanceId);
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
            var device = this.libraryService.GetDevice(deviceKey);
            var mf = await this.libraryService.GetEntityAsync<MusicFile>(musicFileId);
            await QueueEntity<MusicFile>(device, mf);
            return SuccessResult();
        }
        [HttpGet("queue/track/{deviceKey}/{trackId}")]
        public async Task<IActionResult> QueueTrack(string deviceKey, long trackId)
        {
            var device = this.libraryService.GetDevice(deviceKey);
            var track = await this.libraryService.GetEntityAsync<Track>(trackId);
            await QueueEntity<Track>(device, track);
            return SuccessResult();
        }
        [HttpGet("queue/work/{deviceKey}/{workId}")]
        public async Task<IActionResult> QueueWork(string deviceKey, long workId)
        {
            var device = this.libraryService.GetDevice(deviceKey);
            var work = await this.libraryService.GetEntityAsync<Work>(workId);
            await QueueEntity<Work>(device, work);
            return SuccessResult();
        }
        [HttpGet("queue/performance/{deviceKey}/{performanceId}")]
        public async Task<IActionResult> QueuePerformance(string deviceKey, long performanceId)
        {
            var device = this.libraryService.GetDevice(deviceKey);
            var performance = await this.libraryService.GetEntityAsync<Performance>(performanceId);
            await QueueEntity<Performance>(device, performance);
            return SuccessResult();
        }

        [HttpGet("togglePlayPause/{deviceKey}")]
        public async Task<IActionResult> TogglePlayPause(string deviceKey)
        {
            var dr = this.playManager.GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                int sn = 0;
                switch (dr.Status.State)
                {
                    case PlayerStates.Paused:
                    case PlayerStates.Playing:
                        sn = await playManager.TogglePlayPauseAsync(deviceKey);
                        break;
                    case PlayerStates.Idle:
                    case PlayerStates.SilentIdle:
                        sn = await playManager.PlayNextAsync(deviceKey);// await PlayNextItem(deviceKey);
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
            var sn = await playManager.SetPositionAsync(deviceKey, position);
            return SuccessResult(sn);
        }
        [HttpGet("skip/forward/{deviceKey}")]
        public async Task<IActionResult> SkipForward(string deviceKey)
        {
            var sn = await playManager.PlayNextAsync(deviceKey);// await playManager.TogglePlayPause(deviceKey);
            return SuccessResult(sn);
        }
        [HttpGet("skip/back/{deviceKey}")]
        public async Task<IActionResult> SkipBack(string deviceKey)
        {
            var sn = await playManager.PlayNextAsync(deviceKey, true);// await playManager.TogglePlayPause(deviceKey);
            return SuccessResult(sn);
        }
        [HttpGet("set/volume/{deviceKey}/{level}")]
        public async Task<IActionResult> SetVolume(string deviceKey, float level)
        {
            var sn = await playManager.SetVolumeAsync(deviceKey, level);
            return SuccessResult(sn);
        }
        [HttpGet("stream/{musicFileId}")]
        public async Task<IActionResult> StreamAudio(long musicFileId)
        {
            //var mf = await musicDb.MusicFiles.FindAsync(musicFileId);
            var mf = await this.libraryService.GetEntityAsync<MusicFile>(musicFileId);
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
            //var device = await musicDb.Devices.SingleOrDefaultAsync(x => x.KeyName == deviceKey);
            var device = this.libraryService.GetDevice(deviceKey);
            if (device == null)
            {
                var ipAddress = this.Request.HttpContext.GetRemoteIPAddress();
                device = await this.libraryService.CreateWebAudioDevice(deviceKey, ipAddress);
            }
            await this.libraryService.MarkAsSeenAsync(device);
            await SyncDeviceWithPlayManager(device);
            return SuccessResult(device.ToDTO());
        }
        [HttpPost("webaudio/device/status")]
        public async Task<IActionResult> WebAudioDeviceStatus()
        {
            var ds = await this.Request.FromBody<DeviceStatus>();
            await this.playManager.OnDeviceStatusAsync(ds);
            return SuccessResult();
        }
        //
        // manual call only
        [HttpGet("reset/devices")]
        public async Task<IActionResult> ResetAllDevices()
        {
            await this.libraryService.ResetAllDevicesAsync();
            return SuccessResult();
        }
        //
        //[HttpGet("get/playlist/{deviceKey}")]
        //public IActionResult GetDevicePlaylist(string deviceKey)
        //{
        //    var dr = this.playManager.GetDeviceRuntime(deviceKey);
        //    if (dr != null)
        //    {
        //        if (dr.Playlist != null)
        //        {
        //            return SuccessResult(dr.Playlist.Items.Select(x => x.ToDTO()));
        //        }
        //        else
        //        {
        //            log.Error($"device with key {deviceKey} has no playlist");
        //            return ErrorResult($"device with key {deviceKey} has no playlist");
        //        }
        //    }
        //    else
        //    {
        //        log.Error($"device {deviceKey} not in run time");
        //        return ErrorResult($"device {deviceKey} not in run time");
        //    }
        //}
        [HttpGet("select/playlist/{deviceKey}/{playlistId}")]
        public async Task<IActionResult> SelectPlaylist(string deviceKey, long playlistId)
        {
            await this.playManager.SetPlaylistAsync(deviceKey, playlistId);
            return SuccessResult();
        }
        /// <summary>
        /// creates a new playlist with the given name containing the items from the current playlist at the device
        /// the current playlist may be a device list in which case this is discarded
        /// or a non-device list in which case it is replaced
        /// </summary>
        /// <param name="deviceKey"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet("savenew/playlist/{deviceKey}/{name}")]       
        public async Task<IActionResult> SaveNewPlaylist(string deviceKey, string name)
        {
            log.Information($"save new playist {name}  from {deviceKey}");
            await this.playManager.CreateNamedPlaylistFromDeviceAsync(deviceKey, name);
            return SuccessResult();
        }
        [HttpGet("replace/playlist/{deviceKey}/{name}")]
        public IActionResult ReplacePlaylist(string deviceKey, string name)
        {
            log.Information($"replace playist {name} from {deviceKey}");
            return SuccessResult();
        }
        [HttpGet("copy/playlist/{fromDeviceKey}/{toDeviceKey}")]
        public async Task<IActionResult> CopyPlaylist(string fromDeviceKey, string toDeviceKey)
        {
            await playManager.CopyPlaylistAsync(fromDeviceKey, toDeviceKey);
            //await PlayNextItem(toDeviceKey);
            return SuccessResult();
        }
        [HttpGet("get/all/playlist/names")]
        public IActionResult GetAllNonDevicePlaylistNames()
        {
            var list = this.libraryService.GetAllPlaylists()
                .Where(pl => pl.Type != PlaylistType.DeviceList);
            return SuccessResult(list.Select(x => x.Name));
            //return SuccessResult(new string[] {
            //    "alpha",
            //    "beta",
            //    "gamma",
            //    "delta",
            //});
        }
        [HttpGet("get/all/playlists")]
        public IActionResult GetAllNonDevicePlaylists()
        {
            //var rnd = new Random();
            //var count = rnd.Next(4, 15);
            var all = this.libraryService.GetAllPlaylists()
                .Where(pl => pl.Type != PlaylistType.DeviceList);
            var list = all.Select(x => ExtendedPlaylist.Create(x, this.libraryService));
            return SuccessResult(list.Select(x => x.ToDTO()));
            //var dto = list.Select(x => x.ToDTO());
            //var r = Enumerable.Repeat(dto.First(), count);
            //return SuccessResult(r);
        }
        [HttpPost("update/playlist")]
        public async Task<IActionResult> UpdatePlaylist()
        {
            var dto = await this.Request.FromBody<PlaylistDTO>();
            var playlist = await this.libraryService.GetEntityAsync<Playlist>(dto.Id);
            if(playlist != null)
            {
                if(dto.PlaylistName != playlist.Name)
                {
                    playlist.Name = dto.PlaylistName;
                }
                var playlistItems = new List<PlaylistItem>();
                foreach(PlaylistItemDTO itemDto in dto.Items.OrderBy(x => x.Position.Major))
                {
                    var pli = playlist.Items.SingleOrDefault(x => x.Id == itemDto.Id);
                    if(pli != null)
                    {
                        pli.Sequence = itemDto.Position.Major;
                        playlistItems.Add(pli);
                    }
                }
                await this.libraryService.UpdatePlaylistItems(playlist, playlistItems);
                await this.playManager.ResetDevicePlaylistItems(playlist);
                return SuccessResult();
            }
            else
            {
                return ErrorResult($"playlist id {dto.Id} not found");
            }
        }
        [HttpGet("delete/playlist/{id}")]
        public async Task<IActionResult> DeletePlaylist(long id)
        {
            await this.playManager.DeletePlaylistAsync(id);
            //// 1. reset any devices that are currently using this playlist (sets that device's playlist to (auto)
            //// 2. delete the playlist from the system
            //var deviceRuntimes = this.playManager.FindDevicesUsingPlaylist(id);
            //foreach(var dr in deviceRuntimes)
            //{
            //    var device = this.libraryService.GetDevice(dr.Key);
            //    var pl = await this.libraryService.SetEmptyDevicePlaylist(device);
            //    await this.playManager.SetPlaylist(dr, device.Playlist);
            //    //await this.playManager.SetRuntimePlaylist(device, dr);
            //    await this.playManager.SendPlaylist(dr.ToPlaylistDTO());
            //}
            //await this.libraryService.DeletePlaylist(id);
            return SuccessResult();
        }
        [HttpGet("test")]
        public IActionResult Test()
        {
            var playlists = this.libraryService.GetAllPlaylists();
            foreach (var pl in playlists)
            {
                var epl = ExtendedPlaylist.Create(pl, this.libraryService);
                Debug.WriteLine(epl);
                foreach (var item in epl.Items)
                {
                    Debug.WriteLine($"-->{item}");
                    if (item is MultiTrackPlaylistItem)
                    {
                        var m_item = item as MultiTrackPlaylistItem;
                        foreach (var subItem in m_item.SubItems)
                        {
                            Debug.WriteLine($"---->{subItem}");
                        }
                    }
                }
            }
            return new EmptyResult();
        }
        private async Task QueueEntity<T>(Device device, T entity) where T : EntityBase
        {
            await this.playManager.AddPlaylistItemAsync<T>(device.KeyName, entity);
            //this.playManager.DebugDevicePlaylist(device.KeyName);
            //var pli = await this.libraryService.AddPlaylistItem(device.Playlist, entity);
            //await playManager.AddPlaylistItem(device.KeyName, pli);
            //playManager.DebugDevicePlaylist(device.KeyName);
        }
        private async Task<int> PlayEntity<T>(Device device, T entity) where T : EntityBase
        {
            var pl = await this.libraryService.CreateDevicePlaylistAsync(device.KeyName);
            await this.playManager.SetPlaylistAsync(device.KeyName, pl);
            await this.playManager.AddPlaylistItemAsync<T>(device.KeyName, entity);
            //this.playManager.DebugDevicePlaylist(device.KeyName);
            return await playManager.PlayNextAsync(device.KeyName);
        }
        private async Task SyncDeviceWithPlayManager(Device device)
        {
            await playManager.UpdateDeviceRuntimeAsync(device);
            //playManager.DebugDevicePlaylist(device.KeyName);
        }
    }
}