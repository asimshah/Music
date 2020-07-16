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
        #region Private Fields

        private readonly LibraryService libraryService;
        //private readonly MusicServerOptions musicServerOptions;
        private readonly PlayManager playManager;

        #endregion Private Fields

        #region Public Constructors

        public PlayerController(LibraryService libraryService,
            /*IOptions<MusicServerOptions> serverOptions,*/ PlayManager pm,
            ILogger<PlayerController> logger, IWebHostEnvironment env) : base(logger, env)
        {
            //this.musicServerOptions = serverOptions.Value;
            playManager = pm;
            this.libraryService = libraryService;// as ILibraryService;
        }

        #endregion Public Constructors

        #region Public Methods

        [HttpPost("confirm/device")]
        public async Task<IActionResult> ConfirmDevice()
        {
            var deviceToConfirm = await this.Request.FromBody<AudioDevice>();
            Device device = await this.libraryService.ConfirmDeviceAsync(deviceToConfirm);
            await this.playManager.AddDeviceToRuntime(device);
            return SuccessResult(device.ToDTO());
        }
        [HttpGet("webaudio/start/{deviceKey}")]
        public async Task<IActionResult> ConfirmWebAudio(string deviceKey)
        {
            var device = this.libraryService.GetDevice(deviceKey);
            if (device == null)
            {
                var ipAddress = this.Request.HttpContext.GetRemoteIPAddress();
                device = await this.libraryService.CreateWebAudioDevice(deviceKey, ipAddress);
            }
            await this.libraryService.MarkAsSeenAsync(device);
            await this.playManager.AddDeviceToRuntime(device);
            return SuccessResult(device.ToDTO());
        }
        [HttpGet("copy/playlist/{fromDeviceKey}/{toDeviceKey}")]
        public async Task<IActionResult> CopyPlaylist(string fromDeviceKey, string toDeviceKey)
        {
            await playManager.CopyPlaylistAsync(fromDeviceKey, toDeviceKey);
            return SuccessResult();
        }
        [HttpGet("delete/playlist/{id}")]
        public async Task<IActionResult> DeletePlaylist(long id)
        {
            await this.playManager.DeletePlaylistAsync(id);
            return SuccessResult();
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
        [HttpGet("get/all/playlist/names")]
        public IActionResult GetAllNonDevicePlaylistNames()
        {
            var list = this.libraryService.GetAllPlaylists()
                .Where(pl => pl.Type != PlaylistType.DeviceList);
            return SuccessResult(list.Select(x => x.Name));
        }
        [HttpGet("get/all/playlists")]
        public IActionResult GetAllNonDevicePlaylists()
        {
            var all = this.libraryService.GetAllPlaylists()
                .Where(pl => pl.Type != PlaylistType.DeviceList);
            var list = all.Select(x => this.libraryService.CreateExtendedPlaylist(x));
            return SuccessResult(list.Select(x => x.ToDTO()));
        }
        [HttpGet("get/devices/{all}")]
        public IActionResult GetAudioDevices(bool all)
        {
            var devices = this.libraryService.GetAudioDevices(all);
            return SuccessResult(devices.Select(x => x.ToDTO()));
        }
        [HttpGet("get/device/{deviceKey}/status")]
        public IActionResult GetDeviceStatus(string deviceKey)
        {
            return SuccessResult(this.playManager.GetDeviceStatus(deviceKey));
        }
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
            var mf = await this.libraryService.GetEntityAsync<MusicFile>(musicFileId);
            var sn = await PlayEntity<MusicFile>(deviceKey, mf);
            return SuccessResult(sn);
        }
        [HttpGet("play/next/{deviceKey}")]
        public async Task<IActionResult> PlayNextItem(string deviceKey)
        {
            var sn = await playManager.PlayNextAsync(deviceKey);
            return SuccessResult(sn);
        }
        [HttpGet("play/performance/{deviceKey}/{performanceId}")]
        public async Task<IActionResult> PlayPerformance(string deviceKey, long performanceId)
        {
            var performance = await this.libraryService.GetEntityAsync<Performance>(performanceId);
            var sn = await PlayEntity<Performance>(deviceKey, performance);
            return SuccessResult(sn);
        }
        [HttpGet("play/previous/{deviceKey}")]
        public async Task<IActionResult> PlayPreviousItem(string deviceKey)
        {
            var sn = await playManager.PlayNextAsync(deviceKey, true);
            return SuccessResult(sn);
        }
        [HttpGet("play/track/{deviceKey}/{trackId}")]
        public async Task<IActionResult> PlayTrack(string deviceKey, long trackId)
        {
            var track = await this.libraryService.GetEntityAsync<Track>(trackId);
            var sn = await PlayEntity<Track>(deviceKey, track);
            return SuccessResult(sn);
        }
        [HttpGet("play/work/{deviceKey}/{workId}")]
        public async Task<IActionResult> PlayWork(string deviceKey, long workId)
        {
            var work = await this.libraryService.GetEntityAsync<Work>(workId);
            var sn = await PlayEntity<Work>(deviceKey, work);
            return SuccessResult(sn);
        }
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
        [HttpGet("queue/file/{deviceKey}/{musicFileId}")]
        public async Task<IActionResult> QueueMusicFile(string deviceKey, long musicFileId)
        {
            var mf = await this.libraryService.GetEntityAsync<MusicFile>(musicFileId);
            await QueueEntity<MusicFile>(deviceKey, mf);
            return SuccessResult();
        }
        [HttpGet("queue/performance/{deviceKey}/{performanceId}")]
        public async Task<IActionResult> QueuePerformance(string deviceKey, long performanceId)
        {
            var performance = await this.libraryService.GetEntityAsync<Performance>(performanceId);
            await QueueEntity<Performance>(deviceKey, performance);
            return SuccessResult();
        }
        [HttpGet("queue/track/{deviceKey}/{trackId}")]
        public async Task<IActionResult> QueueTrack(string deviceKey, long trackId)
        {
            var track = await this.libraryService.GetEntityAsync<Track>(trackId);
            await QueueEntity<Track>(deviceKey, track);
            return SuccessResult();
        }
        [HttpGet("queue/work/{deviceKey}/{workId}")]
        public async Task<IActionResult> QueueWork(string deviceKey, long workId)
        {
            var work = await this.libraryService.GetEntityAsync<Work>(workId);
            await QueueEntity<Work>(deviceKey, work);
            return SuccessResult();
        }
        [HttpGet("replace/playlist/{deviceKey}/{name}")]
        public IActionResult ReplacePlaylist(string deviceKey, string name)
        {
            log.Information($"replace playist {name} from {deviceKey}");
            return SuccessResult();
        }
        [HttpGet("reposition/{deviceKey}/{position}")]
        public async Task<IActionResult> Reposition(string deviceKey, float position)
        {
            // position is a decimal
            var sn = await playManager.SetPositionAsync(deviceKey, position);
            return SuccessResult(sn);
        }
        [HttpGet("reset/devices")]
        public async Task<IActionResult> ResetAllDevices()
        {
            await this.libraryService.ResetAllDevicesAsync();
            return SuccessResult();
        }
        [HttpGet("savenew/playlist/{deviceKey}/{name}")]
        public async Task<IActionResult> SaveNewPlaylist(string deviceKey, string name)
        {
            log.Information($"save new playist {name}  from {deviceKey}");
            await this.playManager.CreateNamedPlaylistFromDeviceAsync(deviceKey, name);
            return SuccessResult();
        }
        [HttpGet("select/playlist/{deviceKey}/{playlistId}")]
        public async Task<IActionResult> SelectPlaylist(string deviceKey, long playlistId)
        {
            await this.playManager.SetPlaylistAsync(deviceKey, playlistId);
            return SuccessResult();
        }
        [HttpGet("send/device/{deviceKey}/playlist")]
        public async Task<IActionResult> SendDevicePlaylist(string deviceKey)
        {
            await this.playManager.SendDevicePlaylistAsync(deviceKey);
            return SuccessResult();
        }
        [HttpGet("send/device/{deviceKey}/status")]
        public async Task<IActionResult> SendDeviceStatus(string deviceKey)
        {
            await this.playManager.SendDeviceStatus(deviceKey);
            return SuccessResult();
        }
        [HttpGet("set/volume/{deviceKey}/{level}")]
        public async Task<IActionResult> SetVolume(string deviceKey, float level)
        {
            var sn = await playManager.SetVolumeAsync(deviceKey, level);
            return SuccessResult(sn);
        }
        [HttpGet("skip/back/{deviceKey}")]
        public async Task<IActionResult> SkipBack(string deviceKey)
        {
            var sn = await playManager.PlayNextAsync(deviceKey, true);// await playManager.TogglePlayPause(deviceKey);
            return SuccessResult(sn);
        }
        [HttpGet("skip/forward/{deviceKey}")]
        public async Task<IActionResult> SkipForward(string deviceKey)
        {
            var sn = await playManager.PlayNextAsync(deviceKey);// await playManager.TogglePlayPause(deviceKey);
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
        [HttpGet("togglePlayPause/{deviceKey}")]
        public async Task<IActionResult> TogglePlayPause(string deviceKey)
        {
            var sn = await this.playManager.TogglePlayPauseAsync(deviceKey);
            return SuccessResult(sn);
            //var dr = this.playManager.GetDeviceRuntime(deviceKey);
            //if (dr != null)
            //{
            //    int sn = 0;
            //    switch (dr.Status.State)
            //    {
            //        case PlayerStates.Paused:
            //        case PlayerStates.Playing:
            //            sn = await playManager.TogglePlayPauseAsync(deviceKey);
            //            break;
            //        case PlayerStates.Idle:
            //        case PlayerStates.SilentIdle:
            //            sn = await playManager.PlayNextAsync(deviceKey);// await PlayNextItem(deviceKey);
            //            break;
            //        default:
            //            log.Warning($"{dr.DisplayName} TogglePlayPause in state {dr.Status.State}");
            //            break;
            //    }

            //    return SuccessResult(sn);
            //}
            //else
            //{
            //    log.Error($"device {deviceKey} not in run time");
            //    return ErrorResult($"device {deviceKey} not in run time");
            //}
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
        [HttpPost("update/playlist")]
        public async Task<IActionResult> UpdatePlaylist()
        {
            var dto = await this.Request.FromBody<PlaylistDTO>();
            var playlist = await this.libraryService.GetEntityAsync<Playlist>(dto.Id);
            if (playlist != null)
            {
                if (dto.PlaylistName != playlist.Name)
                {
                    playlist.Name = dto.PlaylistName;
                }
                var playlistItems = new List<PlaylistItem>();
                foreach (PlaylistItemDTO itemDto in dto.Items.OrderBy(x => x.Position.Major))
                {
                    var pli = playlist.Items.SingleOrDefault(x => x.Id == itemDto.Id);
                    if (pli != null)
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
        [HttpPost("webaudio/device/status")]
        public async Task<IActionResult> WebAudioDeviceStatus()
        {
            var ds = await this.Request.FromBody<DeviceStatus>();
            await this.playManager.SendDeviceStatus(ds);
            return SuccessResult();
        }

        #endregion Public Methods

        #region Private Methods

        private async Task<int> PlayEntity<T>(string deviceKey, T entity) where T : EntityBase
        {
            var pl = await this.libraryService.CreateDevicePlaylistAsync(deviceKey);
            await this.playManager.SetPlaylistAsync(deviceKey, pl);
            await this.playManager.AddPlaylistItemAsync<T>(deviceKey, entity);
            return await playManager.PlayNextAsync(deviceKey);
        }
        private async Task QueueEntity<T>(string deviceKey, T entity) where T : EntityBase
        {
            await this.playManager.AddPlaylistItemAsync<T>(deviceKey, entity);
        }
        private async Task SyncDeviceWithPlayManager(Device device)
        {
            await playManager.UpdateDeviceRuntimeAsync(device);
        }

        #endregion Private Methods
    }
}