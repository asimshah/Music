using Fastnet.Core;
using Fastnet.Core.Web;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    /// <summary>
    /// singleton service that keeps track of client browsers
    /// </summary>
    public class BrowserMonitor
    {
        public class BrowserData
        {
            public string SignalRConnectionId { get; set; }
            public string BrowserKey { get; set; }
            public string IPAddressString { get; set; }
            public string BrowserName { get; set; }
        }
        /// <summary>
        /// a list by IPAddressString and <something></something>
        /// </summary>
        private IDictionary<string, BrowserData> browsers = new Dictionary<string, BrowserData>();
        private readonly ILogger log;
        private readonly PlayManager playManager;
        public BrowserMonitor(ILogger<BrowserMonitor> logger, PlayManager pm /*Microsoft.Extensions.Hosting.IHostedService hs*/)
        {
            this.log = logger;
            //playManager = (hs as SchedulerService).GetRealtimeTask<PlayManager>();
            playManager = pm;// schedulerService.GetRealtimeTask<PlayManager>();
        }
        public void ConnectBrowser(string signalRConnectionId, string browserKey, string ipAddress, string browserName)
        {
            if(!this.browsers.ContainsKey(signalRConnectionId))
            {
                this.browsers.Add(signalRConnectionId, new BrowserData
                {
                    SignalRConnectionId = signalRConnectionId,
                    BrowserKey = browserKey,
                    IPAddressString = ipAddress,
                    BrowserName = browserName
                });
                log.Debug($"{signalRConnectionId} added: browser key {browserKey}, ip address {ipAddress}, brower name {browserName}");
            }
            else
            {
                log.Error($"signalr connectionId {signalRConnectionId} already exists");
            }
            ValidateBrowsers();
        }
        public async Task DisconnectBrowser(string signalRConnectionId)
        {
            if (this.browsers.ContainsKey(signalRConnectionId))
            {
                var bd = this.browsers[signalRConnectionId];
                this.browsers.Remove(signalRConnectionId);
                await this.playManager.BrowserDisconnectedAsync(bd.BrowserKey);
                log.Debug($"{signalRConnectionId} removed: browser key {bd.BrowserKey}, ip address {bd.IPAddressString}, brower name {bd.BrowserName}");
            }
            else
            {
                log.Error($"signalr connectionId {signalRConnectionId} not found");
            }
            ValidateBrowsers();
        }
        //public async Task SendInitialPlaylist(string signalRConnectionId)
        //{
        //    if (this.browsers.ContainsKey(signalRConnectionId))
        //    {
        //        var bd = this.browsers[signalRConnectionId];
        //        //await this.playManager.SendInitialPlaylist(bd.BrowserKey);
        //    }
        //}
        private void ValidateBrowsers()
        {
            var data = this.browsers.Values.GroupBy((bd) => new { ip = bd.IPAddressString, bn = bd.BrowserName }, (x) => x, (k, v) => new { key = k, list = v} );
            log.Debug("browsers are:");
            foreach(var item in data.OrderBy(x => x.key.ip).ThenBy(x => x.key.bn))
            {
                var key = item.key;
                var list = item.list;
                log.Debug($"    {key.ip}, {key.bn}, count = {list.Count()}");
                foreach(var bd in list)
                {
                    log.Debug($"        signalr connection id {bd.SignalRConnectionId}, browser key {bd.BrowserKey}, {bd.BrowserName}");
                }
            }
        }
    }
}
