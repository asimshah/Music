using Fastnet.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Messages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    // possibly rename this hub as it now handles both player and library messages
    // rename this to MessageHub
    public class MessageHub : Hub<IHubMessage>
    {
        private readonly ILogger log;
        private readonly BrowserMonitor browserMonitor;
        public MessageHub(ILogger<MessageHub> logger, BrowserMonitor bm)
        {
            log = logger;
            this.browserMonitor = bm;
        }
        public override Task OnConnectedAsync()
        {
            //log.Information($"{this.Context.ConnectionId} connected");
            //Clients.Caller.SendConnectionId(this.Context.ConnectionId);
            return base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if(exception != null)
            {
                log.Error(exception);
            }
            //log.Information($"{this.Context.ConnectionId} disconnected");
            await this.browserMonitor.DisconnectBrowser(this.Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
        public void ConnectBrowser(string key, string ipAddress, string browserName)
        {
            var connectionId = this.Context.ConnectionId;
            if(string.IsNullOrWhiteSpace(key))
            {
                key = Guid.NewGuid().ToString().ToLower();
            }
            this.browserMonitor.ConnectBrowser(connectionId, key, ipAddress, browserName);
            //return key;
        }
        public void ConnectWebAudio()
        {
            //log.Information($"ConnectWebAudio() called by{this.Context.ConnectionId}");
            this.Groups.AddToGroupAsync(this.Context.ConnectionId, "WebAudio");
            log.Debug($"connection id {this.Context.ConnectionId} added to WebAudioGroup");
        }
        protected override void Dispose(bool disposing)
        {
            log.Debug($"PlayHub disposing");
            base.Dispose(disposing);
        }
    }
}
