using Fastnet.Core;
using System;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    public partial interface IHubMessage
    {
        Task SendArtistNewOrModified(long id);
        Task SendArtistDeleted(long id);
    }
    public partial class PlayManager
    {
        public async Task SendArtistNewOrModified(long id)
        {
            try
            {
                await this.playHub.Clients.All.SendArtistNewOrModified(id);
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
                await this.playHub.Clients.All.SendArtistDeleted(id);
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }

        }
    }
}
