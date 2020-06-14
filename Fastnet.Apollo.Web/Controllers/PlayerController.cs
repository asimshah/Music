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
        //private readonly MusicDb musicDb;
        private readonly PlayManager playManager;
        private readonly LibraryService libraryService;
        //private readonly ILoggerFactory loggerFactory;
        private readonly MusicServerOptions musicServerOptions;
        //private readonly IHubContext<PlayHub, IHubMessage> playHub;
        public PlayerController(MusicDb mdb,
            LibraryService libraryService, /*IHubContext<MessageHub, IHubMessage> messageHub,*/
            IOptions<MusicServerOptions> serverOptions, PlayManager pm,
            /*ILoggerFactory loggerFactory, */ILogger<PlayerController> logger, IWebHostEnvironment env) : base(logger, env)
        {
            this.musicServerOptions = serverOptions.Value;
            playManager = pm;
            this.libraryService = libraryService;// as ILibraryService;
            //this.musicDb = mdb;
            //this.loggerFactory = loggerFactory;
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
            var sn = await playManager.PlayNext(deviceKey);
            return SuccessResult(sn);
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
            await this.playManager.OnDeviceStatus(ds);
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
        [HttpGet("get/playlist/{deviceKey}")]
        public IActionResult GetDevicePlaylist(string deviceKey)
        {
            var dr = this.playManager.GetDeviceRuntime(deviceKey);
            if (dr != null)
            {
                if (dr.Playlist != null)
                {
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
        [HttpGet("savenew/playlist/{deviceKey}/{name}")]
        public IActionResult SaveNewPlaylist(string deviceKey, string name)
        {
            log.Information($"save new playist {name}  from {deviceKey}");
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
            await playManager.CopyPlaylist(fromDeviceKey, toDeviceKey);
            await PlayNextItem(toDeviceKey);
            return SuccessResult();
        }
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
        private async Task QueueEntity<T>(Device device, T entity) where T : EntityBase
        {
            await LoadDevicePlaylist(device, entity);
        }
        /// <summary>
        /// Always clears any existing playlist and then adds the entity
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="device"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        private async Task<int> PlayEntity<T>(Device device, T entity) where T : EntityBase
        {
            await playManager.ClearPlaylist(device.KeyName);
            await LoadDevicePlaylist(device, entity);
            //playManager.DebugDevicePlaylist(device.KeyName);
            return await playManager.PlayNext(device.KeyName);
        }
        private async Task LoadDevicePlaylist<T>(Device device, T entity) where T : EntityBase
        {
            var playlist = device.Playlist;
            await this.libraryService.ReplacePlaylistItems<T>(playlist, entity);
            await playManager.AddPlaylistItem(device.KeyName, playlist.Items.First());
        }
        //private PlaylistItem CreateNewPlaylistItem(MusicFile mf)
        //{
        //    var pli = new PlaylistItem
        //    {
        //        Type = PlaylistItemType.MusicFile,
        //        Title = mf.Track.Title,
        //        ItemId = mf.Track.Id,
        //        MusicFileId = mf.Id
        //    };
        //    return pli;
        //}
        //private PlaylistItem CreateNewPlaylistItem(Track track)
        //{
        //    var pli = new PlaylistItem
        //    {
        //        Type = PlaylistItemType.Track,
        //        Title = track.Title,
        //        ItemId = track.Id,
        //        MusicFileId = 0
        //    };
        //    return pli;
        //}
        //private PlaylistItem CreateNewPlaylistItem(Work work)
        //{
        //    var pli = new PlaylistItem
        //    {
        //        Type = PlaylistItemType.Work,
        //        Title = work.Name,// mf.Track.Title,
        //        ItemId = work.Id,
        //        MusicFileId = 0// mf.Id
        //    };
        //    return pli;
        //}
        //private PlaylistItem CreateNewPlaylistItem(Performance performance)
        //{
        //    //var title = performance.Composition?.Name ?? performance.RagaPerformances.Select(x => x.Raga).Single().Name;
        //    var pli = new PlaylistItem
        //    {
        //        Type = PlaylistItemType.Performance,
        //        Title = performance.GetParentEntityName(),
        //        ItemId = performance.Id,
        //        MusicFileId = 0// mf.Id
        //    };
        //    return pli;
        //}
        /// <summary>
        /// called from ConfirmDevice() (.. from an agent) or a user edit of the device
        /// fills out runtime playlist info and syncs with the play manager
        /// q1? shoudl playlist be created/validated here??
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        private async Task SyncDeviceWithPlayManager(Device device)
        {
            await playManager.UpdateDeviceRuntime(device);
            //playManager.DebugDevicePlaylist(device.KeyName);
        }

    }

}