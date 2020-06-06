using Fastnet.Core;
using Fastnet.Core.Web;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{


    public partial class LibraryMessages : HostedService
    {
        private CancellationToken cancellationToken;
        private readonly IHubContext<MessageHub, IHubMessage> messageHub;

        public LibraryMessages(IHubContext<MessageHub, IHubMessage> messageHub, ILogger<LibraryMessages> log) : base(log)
        {
            this.messageHub = messageHub;
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
