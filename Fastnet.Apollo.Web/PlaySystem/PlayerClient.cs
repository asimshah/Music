using Fastnet.Core.Web;
using Fastnet.Music.Messages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    public class PlayerClient : WebApiClient
    {
        public PlayerClient(string url, ILogger<PlayerClient> log) : base(url, log)
        {
        }
        public async Task Execute(PlayerCommand command)
        {
            var url = "device/execute";
            await this.PostAsync<PlayerCommand>(url, command);
        }
        public async Task Poll()
        {
            var url = "device/poll";
            await this.GetAsync(url);
        }
    }
}
