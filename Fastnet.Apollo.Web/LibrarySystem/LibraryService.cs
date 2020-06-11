using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Music.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    public partial class LibraryService : HostedService
    {
        private CancellationToken cancellationToken;
        private readonly IHubContext<MessageHub, IHubMessage> messageHub;
        private readonly string connectionString;
        public LibraryService(IWebHostEnvironment environment, IConfiguration cfg, IHubContext<MessageHub, IHubMessage> messageHub, ILogger<LibraryService> log) : base(log)
        {
            this.messageHub = messageHub;
            connectionString = environment.LocaliseConnectionString(cfg.GetConnectionString("MusicDb"));
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
        public void CopyPlaylist(string fromDevicekey, string toDevicekey)
        {
            using (var db = new MusicDb(connectionString))
            {
                var from = db.Devices.Single(x => x.KeyName == fromDevicekey);
                var to = db.Devices.Single(x => x.KeyName == toDevicekey);
                var toRemove = to.Playlist.Items.ToArray();
                db.PlaylistItems.RemoveRange(toRemove);
                foreach(var item in from.Playlist.Items)
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
                    db.PlaylistItems.Add(ni);
                }
                db.SaveChanges();
            }
        }

        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
            try
            {
                await Start();
            }
            catch (AggregateException ae)
            {
                foreach (var xe in ae.InnerExceptions)
                {
                    log.Error($"Aggregated exception {xe.GetType().Name}, {xe.Message}");
                }
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }
        }
        private async Task Start()
        {
            log.Information($"started");
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(5000);
                }
                catch (Exception xe)
                {
                    log.Error(xe);
                }
            }
            log.Information($"cancellation requested");
        }
    }

}
