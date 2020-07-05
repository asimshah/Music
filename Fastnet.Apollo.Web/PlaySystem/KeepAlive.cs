using Fastnet.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    /// <summary>
    /// polls provided urls (which should always be agent urls) regularly
    /// to keep them alive - IIS settings to do this do not appear to work
    /// - or, at least, I can't get them to
    /// </summary>
    public class KeepAgentsAlive
    {
        private Action<string> onPollingFailed;
        private string[] playerUrls;
        private readonly ILoggerFactory lf;
        private readonly MusicServerOptions musicServerOptions;
        private readonly CancellationToken cancellationToken;
        private readonly ILogger log;
        public KeepAgentsAlive(MusicServerOptions serverOptions, CancellationToken cancellationToken, ILoggerFactory loggerFactory)
        {
            this.musicServerOptions = serverOptions;
            this.lf = loggerFactory;
            this.log = lf.CreateLogger<KeepAgentsAlive>();// log;
        }
        public void SetPlayerUrls(string[] playerUrls)
        {
            this.playerUrls = playerUrls;
        }
        public string[] GetPlayerUrls()
        {
            return this.playerUrls;
        }
        public async Task Start(string[] playerUrls, Action<string> onPollingFailed )
        {            
            log.Information($"started, interval is {this.musicServerOptions.KeepAliveInterval} ms");
            this.onPollingFailed = onPollingFailed;
            //await Task.Delay(8000);
            SetPlayerUrls(playerUrls);
            if (this.musicServerOptions.KeepAliveInterval > 0)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (this.playerUrls != null)
                    {
                        await PollPlayers();
                    }
                    await Task.Delay(musicServerOptions.KeepAliveInterval, cancellationToken);
                    //if (cancellationToken.IsCancellationRequested)
                    //{
                    //    break;
                    //}
                }
            }
            else
            {
                log.Warning($"keep alive interval is zero");
            }
            log.Information($"finished");
        }
        internal void OnException(Task task, object arg2)
        {
            if (task.Exception != null)
            {
                log.Error(task.Exception);
            }
            else
            {
                log.Warning($"did not expect to be here!!!!!");
            }
        }
        private async Task PollPlayers()
        {
            try
            {
                foreach (var url in playerUrls.ToArray())
                {
                    try
                    {
                        var pc = new PlayerClient(url, lf.CreateLogger<PlayerClient>());
                        await pc.Poll();
                        log.Trace($"{url} polled");
                    }
                    catch (System.Exception xe)
                    {
                        log.Error($"polling {url} failed: {xe.Message}");
                        this.onPollingFailed(url);
                    }
                }
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }
        }
    }
}
